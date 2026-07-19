namespace OctoMap.Naming
{
    /// <summary>
    /// Normalizes snake_case member names.
    /// </summary>
    public sealed class SnakeCaseNamingConvention : INamingConvention
    {
        /// <summary>
        /// Gets the shared snake_case naming convention instance.
        /// </summary>
        public static SnakeCaseNamingConvention Instance { get; } = new();

        /// <inheritdoc/>
        public string Normalize(string name)
            => new DelimitedNamingConvention('_').Normalize(name);
    }
}
