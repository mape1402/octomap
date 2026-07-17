namespace OctoMap
{
    /// <summary>
    /// Represents contextual information available during a mapping operation.
    /// </summary>
    public interface IMapContext
    {
        /// <summary>
        /// Gets the service provider for the current mapping operation.
        /// </summary>
        IServiceProvider Services { get; }
    }
}
