namespace OctoMap
{
    /// <summary>
    /// Creates mapping contexts for the current mapper scope.
    /// </summary>
    public interface IMapContextFactory
    {
        /// <summary>
        /// Creates a mapping context.
        /// </summary>
        /// <returns>A mapping context instance.</returns>
        IMapContext Create();
    }
}
