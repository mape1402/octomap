using System.Collections.Concurrent;
using OctoMap.Generation;

namespace OctoMap.Runtime
{
    /// <summary>
    /// Caches a compiled two-source map for a closed generic source and destination tuple per registry instance.
    /// </summary>
    /// <typeparam name="TSource1">The first source type.</typeparam>
    /// <typeparam name="TSource2">The second source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    internal static class TypedCompiledTwoSourceMapCache<TSource1, TSource2, TDestination>
    {
        private static readonly ConcurrentDictionary<ICompiledMapRegistry, Lazy<TypedCompiledTwoSourceMap<TSource1, TSource2, TDestination>>> Cache = new(ReferenceEqualityComparer.Instance);
        private static ICompiledMapRegistry cachedRegistry;
        private static TypedCompiledTwoSourceMap<TSource1, TSource2, TDestination> cachedMap;

        /// <summary>
        /// Gets the typed compiled map for the specified registry.
        /// </summary>
        /// <param name="registry">The compiled map registry.</param>
        /// <returns>The typed compiled map.</returns>
        public static TypedCompiledTwoSourceMap<TSource1, TSource2, TDestination> Get(ICompiledMapRegistry registry)
        {
            var currentRegistry = Volatile.Read(ref cachedRegistry);
            var currentMap = Volatile.Read(ref cachedMap);
            if (ReferenceEquals(currentRegistry, registry) && currentMap != null)
            {
                return currentMap;
            }

            var map = Cache.GetOrAdd(
                registry,
                key => new Lazy<TypedCompiledTwoSourceMap<TSource1, TSource2, TDestination>>(
                    () => TypedCompiledTwoSourceMap<TSource1, TSource2, TDestination>.Create(key.GetOrAdd(
                        MultiSourceTypeCache<TSource1, TSource2>.Types,
                        typeof(TDestination))),
                    LazyThreadSafetyMode.ExecutionAndPublication))
                .Value;

            Volatile.Write(ref cachedMap, map);
            Volatile.Write(ref cachedRegistry, registry);
            return map;
        }
    }

    /// <summary>
    /// Stores pre-resolved typed mapper contracts and fallback delegates for a closed generic two-source compiled map.
    /// </summary>
    /// <typeparam name="TSource1">The first source type.</typeparam>
    /// <typeparam name="TSource2">The second source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    internal sealed class TypedCompiledTwoSourceMap<TSource1, TSource2, TDestination>
    {
        private TypedCompiledTwoSourceMap(
            CompiledMap compiledMap,
            IOctoContextFreeMapping<TSource1, TSource2, TDestination> contextFreeMapper,
            IOctoMapping<TSource1, TSource2, TDestination> mapper,
            Func<TSource1, TSource2, TDestination> contextFreeMap,
            Func<TSource1, TSource2, IMapContext, TDestination> map)
        {
            CompiledMap = compiledMap;
            ContextFreeMapper = contextFreeMapper;
            Mapper = mapper;
            ContextFreeMap = contextFreeMap;
            Map = map;
        }

        /// <summary>
        /// Gets the original compiled map metadata.
        /// </summary>
        public CompiledMap CompiledMap { get; }

        /// <summary>
        /// Gets the generated context-free two-source mapper contract when available.
        /// </summary>
        public IOctoContextFreeMapping<TSource1, TSource2, TDestination> ContextFreeMapper { get; }

        /// <summary>
        /// Gets the generated context-aware two-source mapper contract when available.
        /// </summary>
        public IOctoMapping<TSource1, TSource2, TDestination> Mapper { get; }

        /// <summary>
        /// Gets a context-free map delegate when the generated map does not require runtime context.
        /// </summary>
        public Func<TSource1, TSource2, TDestination> ContextFreeMap { get; }

        /// <summary>
        /// Gets a context-aware map delegate.
        /// </summary>
        public Func<TSource1, TSource2, IMapContext, TDestination> Map { get; }

        /// <summary>
        /// Creates a typed compiled map from backend-neutral compiled map metadata.
        /// </summary>
        /// <param name="compiledMap">The compiled map.</param>
        /// <returns>The typed compiled map.</returns>
        public static TypedCompiledTwoSourceMap<TSource1, TSource2, TDestination> Create(CompiledMap compiledMap)
        {
            if (compiledMap == null)
            {
                throw new ArgumentNullException(nameof(compiledMap));
            }

            return new TypedCompiledTwoSourceMap<TSource1, TSource2, TDestination>(
                compiledMap,
                compiledMap.Mapper as IOctoContextFreeMapping<TSource1, TSource2, TDestination>,
                compiledMap.Mapper as IOctoMapping<TSource1, TSource2, TDestination>,
                CreateContextFreeMap(compiledMap),
                CreateMap(compiledMap));
        }

        private static Func<TSource1, TSource2, TDestination> CreateContextFreeMap(CompiledMap compiledMap)
            => null;

        private static Func<TSource1, TSource2, IMapContext, TDestination> CreateMap(CompiledMap compiledMap)
        {
            return (source1, source2, context) => (TDestination)compiledMap.Invoker.Invoke(new object[] { source1, source2, context });
        }
    }
}
