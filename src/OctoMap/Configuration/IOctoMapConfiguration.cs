namespace OctoMap
{
    /// <summary>
    /// Represents immutable OctoMap configuration.
    /// </summary>
    public interface IOctoMapConfiguration
    {
        /// <summary>
        /// Finds a configured type map.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <returns>The matching type map when configured; otherwise, null.</returns>
        ITypeMap FindMap(Type sourceType, Type destinationType);
    }
}
