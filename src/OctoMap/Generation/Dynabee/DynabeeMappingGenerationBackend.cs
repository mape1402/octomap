using System.Reflection.Emit;
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
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Callvirt, assignment.SourceProperty.GetMethod);
                il.Emit(OpCodes.Callvirt, assignment.DestinationProperty.SetMethod);
            }

            il.Emit(OpCodes.Ldloc, destinationLocal);
            il.Emit(OpCodes.Ret);
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
