using OctoMap.Runtime;
using OctoMap.Generation;

namespace OctoMap
{
    /// <summary>
    /// Provides typed runtime mapper access through the compiled map registry.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    internal sealed class OctoMapper<TSource, TDestination> : IOctoMapper<TSource, TDestination>
    {
        private readonly ICompiledMapRegistry _compiledMapRegistry;

        /// <summary>
        /// Initializes a new instance of the <see cref="OctoMapper{TSource, TDestination}"/> class.
        /// </summary>
        /// <param name="compiledMapRegistry">The compiled map registry.</param>
        public OctoMapper(ICompiledMapRegistry compiledMapRegistry)
        {
            _compiledMapRegistry = compiledMapRegistry ?? throw new ArgumentNullException(nameof(compiledMapRegistry));
        }

        /// <inheritdoc/>
        public TDestination Map(TSource source, IMapContext context)
        {
            var typedMap = TypedCompiledMapCache<TSource, TDestination>.Get(_compiledMapRegistry);
            if (typedMap.ContextFreeMapper != null)
            {
                return typedMap.ContextFreeMapper.MapContextFree(source);
            }

            if (typedMap.Mapper != null)
            {
                return typedMap.Mapper.Map(source, context);
            }

            if (typedMap.ContextFreeMap != null)
            {
                return typedMap.ContextFreeMap(source);
            }

            return typedMap.Map(source, context);
        }

        /// <inheritdoc/>
        public TDestination Map(TSource source, TDestination destination, IMapContext context)
        {
            var typedMap = TypedCompiledMapCache<TSource, TDestination>.Get(_compiledMapRegistry);
            var compiledMap = typedMap.CompiledMap;
            if (compiledMap.ExistingDestinationInvoker == null)
            {
                throw new InvalidOperationException($"Compiled map '{compiledMap.MapperType.FullName}' does not support existing destination invocation.");
            }

            if (typedMap.ContextFreeExistingDestinationMapper != null)
            {
                return typedMap.ContextFreeExistingDestinationMapper.MapToExistingContextFree(source, destination);
            }

            if (typedMap.ExistingDestinationMapper != null)
            {
                return typedMap.ExistingDestinationMapper.MapToExisting(source, destination, context);
            }

            if (typedMap.ContextFreeExistingDestinationMap != null)
            {
                return typedMap.ContextFreeExistingDestinationMap(source, destination);
            }

            return typedMap.ExistingDestinationMap(source, destination, context);
        }
    }
}
