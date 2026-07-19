namespace OctoMap.Naming
{
    /// <summary>
    /// Normalizes kebab-case member names.
    /// </summary>
    public sealed class KebabCaseNamingConvention : INamingConvention
    {
        /// <summary>
        /// Gets the shared kebab-case naming convention instance.
        /// </summary>
        public static KebabCaseNamingConvention Instance { get; } = new();

        /// <inheritdoc/>
        public string Normalize(string name)
            => new DelimitedNamingConvention('-').Normalize(name);
    }
}
