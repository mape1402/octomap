namespace OctoMap.Naming
{
    /// <summary>
    /// Normalizes names by lowercasing the complete member name without splitting words.
    /// </summary>
    public sealed class ExactNamingConvention : INamingConvention
    {
        /// <summary>
        /// Gets the shared exact naming convention instance.
        /// </summary>
        public static ExactNamingConvention Instance { get; } = new();

        /// <inheritdoc/>
        public string Normalize(string name)
            => (name ?? string.Empty).ToUpperInvariant();
    }
}
