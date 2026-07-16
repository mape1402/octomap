namespace OctoMap.Runtime
{
    /// <summary>
    /// Creates default mapping contexts.
    /// </summary>
    internal sealed class MapContextFactory : IMapContextFactory
    {
        /// <inheritdoc/>
        public IMapContext Create()
            => new MapContext();
    }
}
