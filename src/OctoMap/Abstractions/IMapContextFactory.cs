namespace OctoMap
{
    /// <summary>
    /// Creates per-operation mapping contexts.
    /// </summary>
    public interface IMapContextFactory
    {
        /// <summary>
        /// Creates a new mapping context.
        /// </summary>
        /// <returns>A new mapping context instance.</returns>
        IMapContext Create();
    }
}
