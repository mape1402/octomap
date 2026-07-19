namespace OctoMap.Naming
{
    /// <summary>
    /// Normalizes camelCase member names.
    /// </summary>
    public sealed class CamelCaseNamingConvention : INamingConvention
    {
        /// <summary>
        /// Gets the shared camelCase naming convention instance.
        /// </summary>
        public static CamelCaseNamingConvention Instance { get; } = new();

        /// <inheritdoc/>
        public string Normalize(string name)
            => PascalCaseNamingConvention.Instance.Normalize(name);
    }
}
