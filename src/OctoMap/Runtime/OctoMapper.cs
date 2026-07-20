using OctoMap.Generation;
using OctoMap.Runtime;

namespace OctoMap
{
    /// <summary>
    /// Provides the default runtime mapper implementation.
    /// </summary>
    internal sealed class OctoMapper : IOctoMapper
    {
        private readonly ICompiledMapRegistry _compiledMapRegistry;
        private readonly IMapContextFactory _contextFactory;
        /// <summary>
        /// Initializes a new instance of the <see cref="OctoMapper"/> class.
        /// </summary>
        /// <param name="compiledMapRegistry">The compiled map registry.</param>
        /// <param name="contextFactory">The mapping context factory.</param>
        /// <param name="projectionBuilder">The projection builder.</param>
        public OctoMapper(
            ICompiledMapRegistry compiledMapRegistry,
            IMapContextFactory contextFactory,
            IOctoProjectionBuilder projectionBuilder)
        {
            _compiledMapRegistry = compiledMapRegistry ?? throw new ArgumentNullException(nameof(compiledMapRegistry));
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
            ProjectionBuilder = projectionBuilder ?? throw new ArgumentNullException(nameof(projectionBuilder));
        }

        /// <inheritdoc/>
        public IOctoProjectionBuilder ProjectionBuilder { get; }

        /// <inheritdoc/>
        public TDestination Map<TDestination>(object source)
        {
            if (source == null)
            {
                return default;
            }

            var compiledMap = _compiledMapRegistry.GetOrAdd(source.GetType(), typeof(TDestination));
            if (compiledMap.ContextFreeObjectMap != null)
            {
                return (TDestination)compiledMap.ContextFreeObjectMap(source);
            }

            if (compiledMap.ObjectMap != null)
            {
                return (TDestination)compiledMap.ObjectMap(source, CreateContext(compiledMap));
            }

            return (TDestination)compiledMap.Invoker.Invoke(new object[] { source, CreateContext(compiledMap) });
        }

        /// <inheritdoc/>
        public TDestination Map<TSource, TDestination>(TSource source)
        {
            var typedMap = TypedCompiledMapCache<TSource, TDestination>.Get(_compiledMapRegistry);
            if (typedMap.ContextFreeMapper != null)
            {
                return typedMap.ContextFreeMapper.MapContextFree(source);
            }

            if (typedMap.Mapper != null)
            {
                return typedMap.Mapper.Map(source, CreateContext(typedMap.CompiledMap));
            }

            if (typedMap.ContextFreeMap != null)
            {
                return typedMap.ContextFreeMap(source);
            }

            return typedMap.Map(source, CreateContext(typedMap.CompiledMap));
        }

        /// <inheritdoc/>
        public TDestination Map<TSource1, TSource2, TDestination>(TSource1 source1, TSource2 source2)
        {
            var typedMap = TypedCompiledTwoSourceMapCache<TSource1, TSource2, TDestination>.Get(_compiledMapRegistry);
            if (typedMap.ContextFreeMapper != null)
            {
                return typedMap.ContextFreeMapper.MapContextFree(source1, source2);
            }

            if (typedMap.Mapper != null)
            {
                return typedMap.Mapper.Map(source1, source2, CreateContext(typedMap.CompiledMap));
            }

            if (typedMap.ContextFreeMap != null)
            {
                return typedMap.ContextFreeMap(source1, source2);
            }

            return typedMap.Map(source1, source2, CreateContext(typedMap.CompiledMap));
        }

        /// <inheritdoc/>
        public TDestination Map<TSource1, TSource2, TSource3, TDestination>(TSource1 source1, TSource2 source2, TSource3 source3)
        {
            var compiledMap = _compiledMapRegistry.GetOrAdd(MultiSourceTypeCache<TSource1, TSource2, TSource3>.Types, typeof(TDestination));
            return compiledMap.Mapper is IOctoContextFreeMapping<TSource1, TSource2, TSource3, TDestination> contextFreeMapper
                ? contextFreeMapper.MapContextFree(source1, source2, source3)
                : ((IOctoMapping<TSource1, TSource2, TSource3, TDestination>)compiledMap.Mapper).Map(source1, source2, source3, CreateContext(compiledMap));
        }

        /// <inheritdoc/>
        public TDestination Map<TSource1, TSource2, TSource3, TSource4, TDestination>(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4)
        {
            var compiledMap = _compiledMapRegistry.GetOrAdd(MultiSourceTypeCache<TSource1, TSource2, TSource3, TSource4>.Types, typeof(TDestination));
            return compiledMap.Mapper is IOctoContextFreeMapping<TSource1, TSource2, TSource3, TSource4, TDestination> contextFreeMapper
                ? contextFreeMapper.MapContextFree(source1, source2, source3, source4)
                : ((IOctoMapping<TSource1, TSource2, TSource3, TSource4, TDestination>)compiledMap.Mapper).Map(source1, source2, source3, source4, CreateContext(compiledMap));
        }

        /// <inheritdoc/>
        public TDestination Map<TSource1, TSource2, TSource3, TSource4, TSource5, TDestination>(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5)
        {
            var compiledMap = _compiledMapRegistry.GetOrAdd(MultiSourceTypeCache<TSource1, TSource2, TSource3, TSource4, TSource5>.Types, typeof(TDestination));
            return compiledMap.Mapper is IOctoContextFreeMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TDestination> contextFreeMapper
                ? contextFreeMapper.MapContextFree(source1, source2, source3, source4, source5)
                : ((IOctoMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TDestination>)compiledMap.Mapper).Map(source1, source2, source3, source4, source5, CreateContext(compiledMap));
        }

        /// <inheritdoc/>
        public TDestination Map<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TDestination>(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6)
        {
            var compiledMap = _compiledMapRegistry.GetOrAdd(MultiSourceTypeCache<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6>.Types, typeof(TDestination));
            return compiledMap.Mapper is IOctoContextFreeMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TDestination> contextFreeMapper
                ? contextFreeMapper.MapContextFree(source1, source2, source3, source4, source5, source6)
                : ((IOctoMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TDestination>)compiledMap.Mapper).Map(source1, source2, source3, source4, source5, source6, CreateContext(compiledMap));
        }

        /// <inheritdoc/>
        public TDestination Map<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TDestination>(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7)
        {
            var compiledMap = _compiledMapRegistry.GetOrAdd(MultiSourceTypeCache<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7>.Types, typeof(TDestination));
            return compiledMap.Mapper is IOctoContextFreeMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TDestination> contextFreeMapper
                ? contextFreeMapper.MapContextFree(source1, source2, source3, source4, source5, source6, source7)
                : ((IOctoMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TDestination>)compiledMap.Mapper).Map(source1, source2, source3, source4, source5, source6, source7, CreateContext(compiledMap));
        }

        /// <inheritdoc/>
        public TDestination Map<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TDestination>(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, TSource8 source8)
        {
            var compiledMap = _compiledMapRegistry.GetOrAdd(MultiSourceTypeCache<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8>.Types, typeof(TDestination));
            return compiledMap.Mapper is IOctoContextFreeMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TDestination> contextFreeMapper
                ? contextFreeMapper.MapContextFree(source1, source2, source3, source4, source5, source6, source7, source8)
                : ((IOctoMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TDestination>)compiledMap.Mapper).Map(source1, source2, source3, source4, source5, source6, source7, source8, CreateContext(compiledMap));
        }

        /// <inheritdoc/>
        public TDestination Map<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TDestination>(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, TSource8 source8, TSource9 source9)
        {
            var compiledMap = _compiledMapRegistry.GetOrAdd(MultiSourceTypeCache<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9>.Types, typeof(TDestination));
            return compiledMap.Mapper is IOctoContextFreeMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TDestination> contextFreeMapper
                ? contextFreeMapper.MapContextFree(source1, source2, source3, source4, source5, source6, source7, source8, source9)
                : ((IOctoMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TDestination>)compiledMap.Mapper).Map(source1, source2, source3, source4, source5, source6, source7, source8, source9, CreateContext(compiledMap));
        }

        /// <inheritdoc/>
        public TDestination Map<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TSource10, TDestination>(TSource1 source1, TSource2 source2, TSource3 source3, TSource4 source4, TSource5 source5, TSource6 source6, TSource7 source7, TSource8 source8, TSource9 source9, TSource10 source10)
        {
            var compiledMap = _compiledMapRegistry.GetOrAdd(MultiSourceTypeCache<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TSource10>.Types, typeof(TDestination));
            return compiledMap.Mapper is IOctoContextFreeMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TSource10, TDestination> contextFreeMapper
                ? contextFreeMapper.MapContextFree(source1, source2, source3, source4, source5, source6, source7, source8, source9, source10)
                : ((IOctoMapping<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TSource10, TDestination>)compiledMap.Mapper).Map(source1, source2, source3, source4, source5, source6, source7, source8, source9, source10, CreateContext(compiledMap));
        }

        /// <inheritdoc/>
        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

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
                return typedMap.ExistingDestinationMapper.MapToExisting(source, destination, CreateContext(compiledMap));
            }

            if (typedMap.ContextFreeExistingDestinationMap != null)
            {
                return typedMap.ContextFreeExistingDestinationMap(source, destination);
            }

            return typedMap.ExistingDestinationMap(source, destination, CreateContext(compiledMap));
        }

        /// <inheritdoc/>
        public TDestination Map<TDestination>(SourceSet sources)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            var compiledMap = _compiledMapRegistry.GetOrAdd(sources.SourceTypes, typeof(TDestination));
            if (compiledMap.Invoker == null)
            {
                throw new InvalidOperationException($"Compiled map '{compiledMap.MapperType.FullName}' does not support source set invocation.");
            }

            return (TDestination)compiledMap.Invoker.Invoke(new SourceSetArgumentList(sources, CreateContext(compiledMap)));
        }

        /// <inheritdoc/>
        public void CompileMap<TSource, TDestination>()
            => _compiledMapRegistry.GetOrAdd(typeof(TSource), typeof(TDestination));

        /// <inheritdoc/>
        public void CompileMap<TSource1, TSource2, TDestination>()
            => _compiledMapRegistry.GetOrAdd(MultiSourceTypeCache<TSource1, TSource2>.Types, typeof(TDestination));

        /// <inheritdoc/>
        public void CompileMap<TSource1, TSource2, TSource3, TDestination>()
            => _compiledMapRegistry.GetOrAdd(MultiSourceTypeCache<TSource1, TSource2, TSource3>.Types, typeof(TDestination));

        /// <inheritdoc/>
        public void CompileMap<TSource1, TSource2, TSource3, TSource4, TDestination>()
            => _compiledMapRegistry.GetOrAdd(MultiSourceTypeCache<TSource1, TSource2, TSource3, TSource4>.Types, typeof(TDestination));

        /// <inheritdoc/>
        public void CompileMap<TSource1, TSource2, TSource3, TSource4, TSource5, TDestination>()
            => _compiledMapRegistry.GetOrAdd(MultiSourceTypeCache<TSource1, TSource2, TSource3, TSource4, TSource5>.Types, typeof(TDestination));

        /// <inheritdoc/>
        public void CompileMap<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TDestination>()
            => _compiledMapRegistry.GetOrAdd(MultiSourceTypeCache<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6>.Types, typeof(TDestination));

        /// <inheritdoc/>
        public void CompileMap<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TDestination>()
            => _compiledMapRegistry.GetOrAdd(MultiSourceTypeCache<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7>.Types, typeof(TDestination));

        /// <inheritdoc/>
        public void CompileMap<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TDestination>()
            => _compiledMapRegistry.GetOrAdd(MultiSourceTypeCache<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8>.Types, typeof(TDestination));

        /// <inheritdoc/>
        public void CompileMap<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TDestination>()
            => _compiledMapRegistry.GetOrAdd(MultiSourceTypeCache<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9>.Types, typeof(TDestination));

        /// <inheritdoc/>
        public void CompileMap<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TSource10, TDestination>()
            => _compiledMapRegistry.GetOrAdd(MultiSourceTypeCache<TSource1, TSource2, TSource3, TSource4, TSource5, TSource6, TSource7, TSource8, TSource9, TSource10>.Types, typeof(TDestination));

        /// <inheritdoc/>
        public void CompileMap<TDestination>(params Type[] sourceTypes)
        {
            if (sourceTypes == null)
            {
                throw new ArgumentNullException(nameof(sourceTypes));
            }

            _compiledMapRegistry.GetOrAdd(sourceTypes, typeof(TDestination));
        }

        /// <inheritdoc/>
        public void CompileMappings()
            => _compiledMapRegistry.CompileConfiguredMaps();

        private IMapContext CreateContext(CompiledMap compiledMap)
            => compiledMap.RequiresContext ? _contextFactory.Create() : null;

        /// <summary>
        /// Provides dynamic source-set arguments without allocating a new object array per map call.
        /// </summary>
        private sealed class SourceSetArgumentList : IReadOnlyList<object>
        {
            private readonly SourceSet _sources;
            private readonly IMapContext _context;

            /// <summary>
            /// Initializes a new instance of the <see cref="SourceSetArgumentList"/> class.
            /// </summary>
            /// <param name="sources">The source set.</param>
            /// <param name="context">The mapping context.</param>
            public SourceSetArgumentList(SourceSet sources, IMapContext context)
            {
                _sources = sources ?? throw new ArgumentNullException(nameof(sources));
                _context = context;
            }

            /// <inheritdoc/>
            public int Count => _sources.Sources.Count + 1;

            /// <inheritdoc/>
            public object this[int index]
            {
                get
                {
                    var sourceCount = _sources.Sources.Count;
                    if ((uint)index < (uint)sourceCount)
                    {
                        return _sources.Sources[index];
                    }

                    if (index == sourceCount)
                    {
                        return _context;
                    }

                    throw new ArgumentOutOfRangeException(nameof(index));
                }
            }

            /// <inheritdoc/>
            public IEnumerator<object> GetEnumerator()
            {
                for (var index = 0; index < Count; index++)
                {
                    yield return this[index];
                }
            }

            /// <inheritdoc/>
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
                => GetEnumerator();
        }
    }
}
