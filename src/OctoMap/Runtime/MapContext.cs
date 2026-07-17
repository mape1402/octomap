namespace OctoMap.Runtime
{
    /// <summary>
    /// Represents the default mapping context.
    /// </summary>
    internal sealed class MapContext : IMapContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MapContext"/> class.
        /// </summary>
        /// <param name="services">The service provider for the current mapping operation.</param>
        public MapContext(IServiceProvider services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
        }

        /// <inheritdoc/>
        public IServiceProvider Services { get; }
    }
}
