namespace OctoMap.Runtime
{
    /// <summary>
    /// Creates default mapping contexts.
    /// </summary>
    internal sealed class MapContextFactory : IMapContextFactory
    {
        private readonly IServiceProvider _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="MapContextFactory"/> class.
        /// </summary>
        /// <param name="services">The service provider for the current scope.</param>
        public MapContextFactory(IServiceProvider services)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <inheritdoc/>
        public IMapContext Create()
            => new MapContext(_services);
    }
}
