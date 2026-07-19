using System.Collections.Concurrent;
using OctoMap.Generation;

namespace OctoMap.Runtime
{
    /// <summary>
    /// Caches a compiled map for a closed generic source and destination pair per registry instance.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    internal static class TypedCompiledMapCache<TSource, TDestination>
    {
        private static readonly ConcurrentDictionary<ICompiledMapRegistry, Lazy<TypedCompiledMap<TSource, TDestination>>> Cache = new(ReferenceEqualityComparer.Instance);
        private static ICompiledMapRegistry cachedRegistry;
        private static TypedCompiledMap<TSource, TDestination> cachedMap;

        /// <summary>
        /// Gets the typed compiled map for the specified registry.
        /// </summary>
        /// <param name="registry">The compiled map registry.</param>
        /// <returns>The typed compiled map.</returns>
        public static TypedCompiledMap<TSource, TDestination> Get(ICompiledMapRegistry registry)
        {
            var currentRegistry = Volatile.Read(ref cachedRegistry);
            var currentMap = Volatile.Read(ref cachedMap);
            if (ReferenceEquals(currentRegistry, registry) && currentMap != null)
            {
                return currentMap;
            }

            var map = Cache.GetOrAdd(
                registry,
                key => new Lazy<TypedCompiledMap<TSource, TDestination>>(
                    () => TypedCompiledMap<TSource, TDestination>.Create(key.GetOrAdd(typeof(TSource), typeof(TDestination))),
                    LazyThreadSafetyMode.ExecutionAndPublication))
                .Value;

            Volatile.Write(ref cachedMap, map);
            Volatile.Write(ref cachedRegistry, registry);
            return map;
        }
    }

    /// <summary>
    /// Stores pre-resolved delegates for a closed generic compiled map.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    internal sealed class TypedCompiledMap<TSource, TDestination>
    {
        private TypedCompiledMap(
            CompiledMap compiledMap,
            Func<TSource, TDestination> contextFreeMap,
            Func<TSource, IMapContext, TDestination> map,
            Func<TSource, TDestination, TDestination> contextFreeExistingDestinationMap,
            Func<TSource, TDestination, IMapContext, TDestination> existingDestinationMap)
        {
            CompiledMap = compiledMap;
            ContextFreeMap = contextFreeMap;
            Map = map;
            ContextFreeExistingDestinationMap = contextFreeExistingDestinationMap;
            ExistingDestinationMap = existingDestinationMap;
        }

        /// <summary>
        /// Gets the original compiled map metadata.
        /// </summary>
        public CompiledMap CompiledMap { get; }

        /// <summary>
        /// Gets a context-free map delegate when the generated map does not require runtime context.
        /// </summary>
        public Func<TSource, TDestination> ContextFreeMap { get; }

        /// <summary>
        /// Gets a context-aware map delegate.
        /// </summary>
        public Func<TSource, IMapContext, TDestination> Map { get; }

        /// <summary>
        /// Gets a context-free existing-destination map delegate when available.
        /// </summary>
        public Func<TSource, TDestination, TDestination> ContextFreeExistingDestinationMap { get; }

        /// <summary>
        /// Gets a context-aware existing-destination map delegate.
        /// </summary>
        public Func<TSource, TDestination, IMapContext, TDestination> ExistingDestinationMap { get; }

        /// <summary>
        /// Creates a typed compiled map from backend-neutral compiled map metadata.
        /// </summary>
        /// <param name="compiledMap">The compiled map.</param>
        /// <returns>The typed compiled map.</returns>
        public static TypedCompiledMap<TSource, TDestination> Create(CompiledMap compiledMap)
        {
            if (compiledMap == null)
            {
                throw new ArgumentNullException(nameof(compiledMap));
            }

            return new TypedCompiledMap<TSource, TDestination>(
                compiledMap,
                CreateContextFreeMap(compiledMap),
                CreateMap(compiledMap),
                CreateContextFreeExistingDestinationMap(compiledMap),
                CreateExistingDestinationMap(compiledMap));
        }

        private static Func<TSource, TDestination> CreateContextFreeMap(CompiledMap compiledMap)
        {
            if (compiledMap.Mapper is IGeneratedContextFreeMapper<TSource, TDestination> generatedMapper)
            {
                return generatedMapper.MapContextFree;
            }

            if (compiledMap.ContextFreeInvoker is ICompiledContextFreeMapInvoker<TSource, TDestination> invoker)
            {
                return invoker.Invoke;
            }

            return null;
        }

        private static Func<TSource, IMapContext, TDestination> CreateMap(CompiledMap compiledMap)
        {
            if (compiledMap.Mapper is IGeneratedSingleSourceMapper<TSource, TDestination> generatedMapper)
            {
                return generatedMapper.Map;
            }

            if (compiledMap.TypedInvoker is ICompiledMapInvoker<TSource, TDestination> invoker)
            {
                return invoker.Invoke;
            }

            return (source, context) => (TDestination)compiledMap.Invoker.Invoke(new object[] { source, context });
        }

        private static Func<TSource, TDestination, TDestination> CreateContextFreeExistingDestinationMap(CompiledMap compiledMap)
        {
            if (compiledMap.Mapper is IGeneratedContextFreeMapper<TSource, TDestination> generatedMapper)
            {
                return generatedMapper.MapToExistingContextFree;
            }

            if (compiledMap.ContextFreeExistingDestinationInvoker is ICompiledContextFreeExistingDestinationMapInvoker<TSource, TDestination> invoker)
            {
                return invoker.Invoke;
            }

            return null;
        }

        private static Func<TSource, TDestination, IMapContext, TDestination> CreateExistingDestinationMap(CompiledMap compiledMap)
        {
            if (compiledMap.Mapper is IGeneratedSingleSourceMapper<TSource, TDestination> generatedMapper)
            {
                return generatedMapper.MapToExisting;
            }

            if (compiledMap.TypedExistingDestinationInvoker is ICompiledExistingDestinationMapInvoker<TSource, TDestination> invoker)
            {
                return invoker.Invoke;
            }

            return compiledMap.ExistingDestinationInvoker == null
                ? null
                : (source, destination, context) => (TDestination)compiledMap.ExistingDestinationInvoker.Invoke(new object[] { source, destination, context });
        }
    }
}
