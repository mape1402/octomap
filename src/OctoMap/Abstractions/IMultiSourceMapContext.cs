namespace OctoMap
{
    /// <summary>
    /// Represents a typed expression context for multi-source member mappings.
    /// </summary>
    public interface IMultiSourceMapContext
    {
        /// <summary>
        /// Gets a source expression by type.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <returns>The source expression value.</returns>
        TSource Get<TSource>();
    }
}
