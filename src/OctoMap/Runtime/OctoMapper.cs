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
        public OctoMapper(ICompiledMapRegistry compiledMapRegistry, IMapContextFactory contextFactory)
        {
            _compiledMapRegistry = compiledMapRegistry ?? throw new ArgumentNullException(nameof(compiledMapRegistry));
            _contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        }

        /// <inheritdoc/>
        public TDestination Map<TDestination>(object source)
        {
            if (source == null)
            {
                return default;
            }

            var compiledMap = _compiledMapRegistry.GetOrAdd(source.GetType(), typeof(TDestination));
            var mapperInterface = typeof(IOctoMapper<,>).MakeGenericType(source.GetType(), typeof(TDestination));
            var method = mapperInterface.GetMethod(nameof(IOctoMapper<object, object>.Map));
            return (TDestination)method.Invoke(compiledMap.Mapper, new[] { source, _contextFactory.Create() });
        }

        /// <inheritdoc/>
        public TDestination Map<TSource, TDestination>(TSource source)
        {
            var compiledMap = _compiledMapRegistry.GetOrAdd(typeof(TSource), typeof(TDestination));
            var mapper = (IOctoMapper<TSource, TDestination>)compiledMap.Mapper;
            return mapper.Map(source, _contextFactory.Create());
        }

        /// <inheritdoc/>
        public TDestination Map<TDestination>(SourceSet sources)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            var compiledMap = _compiledMapRegistry.GetOrAdd(sources.SourceTypes, typeof(TDestination));
            if (compiledMap.SourceSetInvoker == null)
            {
                throw new InvalidOperationException($"Compiled map '{compiledMap.MapperType.FullName}' does not support source set invocation.");
            }

            return (TDestination)compiledMap.SourceSetInvoker(sources, _contextFactory.Create());
        }
    }
}
