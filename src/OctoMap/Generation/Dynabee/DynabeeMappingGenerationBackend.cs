using System.Reflection.Emit;
using System.Linq.Expressions;
using System.Reflection;
using DynaBee.FluentApi;
using OctoMap.Planning;

namespace OctoMap.Generation.Dynabee
{
    /// <summary>
    /// Compiles mapping plans using DynaBee runtime type generation.
    /// </summary>
    internal sealed class DynabeeMappingGenerationBackend : IMappingGenerationBackend
    {
        private int _sequence;

        /// <inheritdoc/>
        public string Name => "Dynabee";

        /// <inheritdoc/>
        public bool Supports(MappingPlan plan)
            => plan != null;

        /// <inheritdoc/>
        public CompiledMap Compile(MappingPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var className = BuildClassName(plan);
            var mapperInterface = typeof(IOctoMapper<,>).MakeGenericType(plan.SourceType, plan.DestinationType);
            var context = DynaBeeBuilder
                .CreateAssembly($"OctoMap.Generated.{Interlocked.Increment(ref _sequence)}")
                .DisableCache()
                .AddClass(className, c => c
                    .Implements(mapperInterface, false)
                    .RegisterAsConcrete(false)
                    .AddMethod(nameof(IOctoMapper<object, object>.Map), plan.DestinationType, m => m
                        .WithParameter("source", plan.SourceType)
                        .WithParameter<IMapContext>("context")
                        .Emits(il => EmitMapMethod(il, plan))))
                .Build();

            var mapperType = context.Find(className).ClrType;
            var mapper = Activator.CreateInstance(mapperType);
            return new CompiledMap(mapper, mapperType);
        }

        private static string BuildClassName(MappingPlan plan)
        {
            var sourceName = Sanitize(plan.SourceType.Name);
            var destinationName = Sanitize(plan.DestinationType.Name);
            return $"{sourceName}To{destinationName}Mapper";
        }

        private static string Sanitize(string value)
        {
            var chars = value.Select(x => char.IsLetterOrDigit(x) ? x : '_').ToArray();
            return new string(chars);
        }

        private static void EmitMapMethod(ILGenerator il, MappingPlan plan)
        {
            var destinationLocal = il.DeclareLocal(plan.DestinationType);

            if (!plan.SourceType.IsValueType)
            {
                var sourceNotNull = il.DefineLabel();
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Brtrue_S, sourceNotNull);
                EmitDefault(il, plan.DestinationType);
                il.Emit(OpCodes.Ret);
                il.MarkLabel(sourceNotNull);
            }

            EmitCreateDestination(il, plan.DestinationType, destinationLocal);

            foreach (var assignment in plan.Assignments)
            {
                il.Emit(OpCodes.Ldloc, destinationLocal);
                EmitAssignmentValue(il, assignment);
                il.Emit(OpCodes.Callvirt, assignment.DestinationProperty.SetMethod);
            }

            il.Emit(OpCodes.Ldloc, destinationLocal);
            il.Emit(OpCodes.Ret);
        }

        private static void EmitAssignmentValue(ILGenerator il, MemberAssignmentPlan assignment)
        {
            if (assignment.SourceExpression != null)
            {
                EmitExpression(il, assignment.SourceExpression.Body, assignment.SourceExpression.Parameters[0]);
                return;
            }

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Callvirt, assignment.SourceProperty.GetMethod);
        }

        private static void EmitExpression(ILGenerator il, Expression expression, ParameterExpression sourceParameter)
        {
            switch (expression)
            {
                case ParameterExpression parameter when ReferenceEquals(parameter, sourceParameter):
                    il.Emit(OpCodes.Ldarg_1);
                    return;
                case MemberExpression member:
                    EmitMemberExpression(il, member, sourceParameter);
                    return;
                case ConstantExpression constant:
                    EmitConstant(il, constant);
                    return;
                case BinaryExpression binary:
                    EmitBinaryExpression(il, binary, sourceParameter);
                    return;
                case UnaryExpression unary when unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked:
                    EmitExpression(il, unary.Operand, sourceParameter);
                    EmitConversion(il, unary.Operand.Type, unary.Type);
                    return;
                default:
                    throw new NotSupportedException($"Expression node '{expression.NodeType}' is not supported by OctoMap Phase 2.");
            }
        }

        private static void EmitMemberExpression(ILGenerator il, MemberExpression expression, ParameterExpression sourceParameter)
        {
            if (expression.Expression != null)
            {
                EmitExpression(il, expression.Expression, sourceParameter);
            }

            if (expression.Member is PropertyInfo property)
            {
                il.Emit(property.GetMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, property.GetMethod);
                return;
            }

            if (expression.Member is FieldInfo field)
            {
                il.Emit(field.IsStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, field);
                return;
            }

            throw new NotSupportedException($"Member '{expression.Member.Name}' is not supported by OctoMap Phase 2.");
        }

        private static void EmitBinaryExpression(ILGenerator il, BinaryExpression expression, ParameterExpression sourceParameter)
        {
            EmitExpression(il, expression.Left, sourceParameter);
            EmitExpression(il, expression.Right, sourceParameter);

            if (expression.Method != null)
            {
                il.Emit(expression.Method.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, expression.Method);
                return;
            }

            switch (expression.NodeType)
            {
                case ExpressionType.Add:
                    il.Emit(OpCodes.Add);
                    return;
                default:
                    throw new NotSupportedException($"Binary expression '{expression.NodeType}' is not supported by OctoMap Phase 2.");
            }
        }

        private static void EmitConstant(ILGenerator il, ConstantExpression expression)
        {
            if (expression.Value == null)
            {
                il.Emit(OpCodes.Ldnull);
                return;
            }

            if (expression.Value is string text)
            {
                il.Emit(OpCodes.Ldstr, text);
                return;
            }

            if (expression.Value is int intValue)
            {
                il.Emit(OpCodes.Ldc_I4, intValue);
                return;
            }

            if (expression.Value is bool boolValue)
            {
                il.Emit(boolValue ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                return;
            }

            throw new NotSupportedException($"Constant type '{expression.Value.GetType().FullName}' is not supported by OctoMap Phase 2.");
        }

        private static void EmitConversion(ILGenerator il, Type fromType, Type toType)
        {
            if (fromType == toType)
            {
                return;
            }

            if (toType == typeof(object))
            {
                if (fromType.IsValueType)
                {
                    il.Emit(OpCodes.Box, fromType);
                }

                return;
            }

            if (!toType.IsValueType)
            {
                il.Emit(OpCodes.Castclass, toType);
                return;
            }

            throw new NotSupportedException($"Conversion from '{fromType.FullName}' to '{toType.FullName}' is not supported by OctoMap Phase 2.");
        }

        private static void EmitCreateDestination(ILGenerator il, Type destinationType, LocalBuilder destinationLocal)
        {
            if (destinationType.IsValueType)
            {
                il.Emit(OpCodes.Ldloca_S, destinationLocal);
                il.Emit(OpCodes.Initobj, destinationType);
                return;
            }

            var constructor = destinationType.GetConstructor(Type.EmptyTypes);
            il.Emit(OpCodes.Newobj, constructor);
            il.Emit(OpCodes.Stloc, destinationLocal);
        }

        private static void EmitDefault(ILGenerator il, Type type)
        {
            if (!type.IsValueType)
            {
                il.Emit(OpCodes.Ldnull);
                return;
            }

            var local = il.DeclareLocal(type);
            il.Emit(OpCodes.Ldloca_S, local);
            il.Emit(OpCodes.Initobj, type);
            il.Emit(OpCodes.Ldloc, local);
        }
    }
}
