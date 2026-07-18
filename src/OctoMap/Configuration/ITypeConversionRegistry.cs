namespace OctoMap.Configuration
{
    /// <summary>
    /// Provides configured and built-in type conversions.
    /// </summary>
    internal interface ITypeConversionRegistry
    {
        /// <summary>
        /// Gets all configured explicit conversions.
        /// </summary>
        IReadOnlyCollection<TypeConversionMap> Conversions { get; }

        /// <summary>
        /// Attempts to find a conversion between the specified types.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <param name="conversion">The resolved conversion.</param>
        /// <returns>True when a conversion is available.</returns>
        bool TryFind(Type sourceType, Type destinationType, out TypeConversionMap conversion);
    }
}
