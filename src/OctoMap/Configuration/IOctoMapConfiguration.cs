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

        /// <summary>
        /// Finds a configured multi-source type map.
        /// </summary>
        /// <param name="sourceTypes">The source types.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <returns>The matching type map when configured; otherwise, null.</returns>
        ITypeMap FindMap(IReadOnlyList<Type> sourceTypes, Type destinationType);

        /// <summary>
        /// Validates the current configuration.
        /// </summary>
        /// <returns>The validation report.</returns>
        OctoMapValidationReport Validate();

        /// <summary>
        /// Throws when the current configuration is invalid.
        /// </summary>
        void AssertValid();
    }
}
