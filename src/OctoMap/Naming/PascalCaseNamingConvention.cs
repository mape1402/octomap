namespace OctoMap.Naming
{
    /// <summary>
    /// Normalizes PascalCase member names.
    /// </summary>
    public sealed class PascalCaseNamingConvention : INamingConvention
    {
        /// <summary>
        /// Gets the shared PascalCase naming convention instance.
        /// </summary>
        public static PascalCaseNamingConvention Instance { get; } = new();

        /// <inheritdoc/>
        public string Normalize(string name)
            => string.Concat(SplitWords(name).Select(x => x.ToUpperInvariant()));

        internal static IReadOnlyList<string> SplitWords(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return Array.Empty<string>();
            }

            var words = new List<string>();
            var start = 0;
            for (var index = 1; index < name.Length; index++)
            {
                var current = name[index];
                var previous = name[index - 1];
                var next = index + 1 < name.Length ? name[index + 1] : '\0';
                if (char.IsUpper(current)
                    && (!char.IsUpper(previous) || (next != '\0' && char.IsLower(next))))
                {
                    words.Add(name[start..index]);
                    start = index;
                }
            }

            words.Add(name[start..]);
            return words;
        }
    }
}
