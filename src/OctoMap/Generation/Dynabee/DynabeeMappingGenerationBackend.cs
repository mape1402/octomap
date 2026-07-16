using System.Linq.Expressions;
using System.Reflection;
using DynaBee.FluentApi;
using DynaBee.FluentApi.Body;
using DynaBee.FluentApi.DependencyInjection;
using OctoMap.Planning;

namespace OctoMap.Generation.Dynabee
{
    /// <summary>
    /// Compiles mapping plans using DynaBee runtime type generation.
    /// </summary>
    internal sealed class DynabeeMappingGenerationBackend : IMappingGenerationBackend
    {
        private readonly IDynaBeeAssemblyBuilderFactory _builderFactory;
        private int _sequence;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynabeeMappingGenerationBackend"/> class.
        /// </summary>
        /// <param name="builderFactory">The DynaBee assembly builder factory.</param>
        public DynabeeMappingGenerationBackend(IDynaBeeAssemblyBuilderFactory builderFactory)
        {
            _builderFactory = builderFactory ?? throw new ArgumentNullException(nameof(builderFactory));
        }

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
            var context = _builderFactory
                .Create($"OctoMap.Generated.{Interlocked.Increment(ref _sequence)}")
                .DisableCache()
                .AddClass(className, c => c
                    .Implements(mapperInterface, false)
                    .RegisterAsConcrete(false)
                    .AddMethod(nameof(IOctoMapper<object, object>.Map), plan.DestinationType, m => m
                        .WithParameter("source", plan.SourceType)
                        .WithParameter<IMapContext>("context")
                        .EmitsBody(body => EmitMapMethod(body, plan))))
                .Build();

            var mapper = context.CreateInstance(className);
            return new CompiledMap(mapper, mapper.GetType());
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

        private static void EmitMapMethod(IBeeMethodBodyBuilder body, MappingPlan plan)
        {
            var source = body.Parameter("source");
            var destination = body.DeclareLocal("destination", plan.DestinationType);

            if (!plan.SourceType.IsValueType)
            {
                body.If(
                    body.IsNull(source),
                    whenTrue => whenTrue.Return(body.Default(plan.DestinationType)));
            }

            body.Assign(destination, CreateDestination(body, plan.DestinationType));

            foreach (var assignment in plan.Assignments)
            {
                var target = body.Property(destination, assignment.DestinationProperty.Name);
                var value = BuildAssignmentValue(body, source, assignment);
                body.Assign(target, value);
            }

            body.Return(destination);
        }

        private static IBeeValueExpression CreateDestination(IBeeMethodBodyBuilder body, Type destinationType)
        {
            return destinationType.IsValueType
                ? body.Default(destinationType)
                : body.New(destinationType);
        }

        private static IBeeValueExpression BuildAssignmentValue(
            IBeeMethodBodyBuilder body,
            IBeeValueExpression source,
            MemberAssignmentPlan assignment)
        {
            IBeeValueExpression value;
            if (assignment.HasConstantValue)
            {
                value = body.Constant(assignment.ConstantValue, assignment.DestinationProperty.PropertyType);
            }
            else if (assignment.SourceExpression != null)
            {
                value = BuildExpression(body, source, assignment.SourceExpression.Body, assignment.SourceExpression.Parameters[0]);
            }
            else
            {
                value = body.Property(source, assignment.SourceProperty.Name);
            }

            if (assignment.HasNullSubstitute)
            {
                value = ApplyNullSubstitute(body, value, assignment);
            }

            return value.Type == assignment.DestinationProperty.PropertyType
                ? value
                : body.Convert(value, assignment.DestinationProperty.PropertyType);
        }

        private static IBeeValueExpression BuildExpression(
            IBeeMethodBodyBuilder body,
            IBeeValueExpression source,
            Expression expression,
            ParameterExpression sourceParameter)
        {
            switch (expression)
            {
                case ParameterExpression parameter when ReferenceEquals(parameter, sourceParameter):
                    return source;
                case MemberExpression member:
                    return BuildMemberExpression(body, source, member, sourceParameter);
                case ConstantExpression constant:
                    return body.Constant(constant.Value, constant.Type);
                case BinaryExpression binary:
                    return BuildBinaryExpression(body, source, binary, sourceParameter);
                case UnaryExpression unary when unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked:
                    return body.Convert(BuildExpression(body, source, unary.Operand, sourceParameter), unary.Type);
                case ConditionalExpression conditional:
                    return body.If(
                        BuildExpression(body, source, conditional.Test, sourceParameter),
                        BuildExpression(body, source, conditional.IfTrue, sourceParameter),
                        BuildExpression(body, source, conditional.IfFalse, sourceParameter));
                default:
                    throw new NotSupportedException($"Expression node '{expression.NodeType}' is not supported by OctoMap Phase 2.");
            }
        }

        private static IBeeValueExpression BuildMemberExpression(
            IBeeMethodBodyBuilder body,
            IBeeValueExpression source,
            MemberExpression expression,
            ParameterExpression sourceParameter)
        {
            if (expression.Member is PropertyInfo property)
            {
                if (expression.Expression == null)
                {
                    return body.StaticProperty(property.DeclaringType, property.Name);
                }

                return body.Property(BuildExpression(body, source, expression.Expression, sourceParameter), property.Name);
            }

            if (expression.Member is FieldInfo field)
            {
                if (expression.Expression == null)
                {
                    return body.StaticField(field.DeclaringType, field.Name);
                }

                return body.Field(BuildExpression(body, source, expression.Expression, sourceParameter), field.Name);
            }

            throw new NotSupportedException($"Member '{expression.Member.Name}' is not supported by OctoMap Phase 2.");
        }

        private static IBeeValueExpression BuildBinaryExpression(
            IBeeMethodBodyBuilder body,
            IBeeValueExpression source,
            BinaryExpression expression,
            ParameterExpression sourceParameter)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Add:
                    return body.Add(
                        BuildExpression(body, source, expression.Left, sourceParameter),
                        BuildExpression(body, source, expression.Right, sourceParameter));
                case ExpressionType.Equal:
                    return body.Equal(
                        BuildExpression(body, source, expression.Left, sourceParameter),
                        BuildExpression(body, source, expression.Right, sourceParameter));
                case ExpressionType.NotEqual:
                    return body.NotEqual(
                        BuildExpression(body, source, expression.Left, sourceParameter),
                        BuildExpression(body, source, expression.Right, sourceParameter));
                default:
                    throw new NotSupportedException($"Binary expression '{expression.NodeType}' is not supported by OctoMap Phase 2.");
            }
        }

        private static IBeeValueExpression ApplyNullSubstitute(
            IBeeMethodBodyBuilder body,
            IBeeValueExpression value,
            MemberAssignmentPlan assignment)
        {
            if (assignment.DestinationProperty.PropertyType.IsValueType)
            {
                throw new NotSupportedException("NullSubstitute for value types is not supported yet.");
            }

            return body.If(
                body.IsNull(value),
                body.Constant(assignment.NullSubstitute, assignment.DestinationProperty.PropertyType),
                value);
        }
    }
}
