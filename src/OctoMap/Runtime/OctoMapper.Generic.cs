using OctoMap.Runtime;

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
            var compiledMap = _compiledMapRegistry.GetOrAdd(typeof(TSource), typeof(TDestination));
            var mapper = (IOctoMapper<TSource, TDestination>)compiledMap.Mapper;
            return mapper.Map(source, context);
        }
    }
}
