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
            return (TDestination)compiledMap.Invoker.Invoke(new object[] { source, _contextFactory.Create() });
        }

        /// <inheritdoc/>
        public TDestination Map<TSource, TDestination>(TSource source)
        {
            var compiledMap = _compiledMapRegistry.GetOrAdd(typeof(TSource), typeof(TDestination));
            return (TDestination)compiledMap.Invoker.Invoke(new object[] { source, _contextFactory.Create() });
        }

        /// <inheritdoc/>
        public TDestination Map<TSource, TDestination>(TSource source, TDestination destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            var compiledMap = _compiledMapRegistry.GetOrAdd(typeof(TSource), typeof(TDestination));
            if (compiledMap.ExistingDestinationInvoker == null)
            {
                throw new InvalidOperationException($"Compiled map '{compiledMap.MapperType.FullName}' does not support existing destination invocation.");
            }

            return (TDestination)compiledMap.ExistingDestinationInvoker.Invoke(new object[] { source, destination, _contextFactory.Create() });
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

            return (TDestination)compiledMap.Invoker.Invoke(sources.Sources.Concat(new object[] { _contextFactory.Create() }).ToArray());
        }

    }
}
