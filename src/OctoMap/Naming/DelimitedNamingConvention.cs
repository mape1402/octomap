namespace OctoMap.Naming
{
    /// <summary>
    /// Normalizes names by splitting them with a delimiter.
    /// </summary>
    public sealed class DelimitedNamingConvention : INamingConvention
    {
        private readonly char _delimiter;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelimitedNamingConvention"/> class.
        /// </summary>
        /// <param name="delimiter">The word delimiter.</param>
        public DelimitedNamingConvention(char delimiter)
        {
            _delimiter = delimiter;
        }

        /// <inheritdoc/>
        public string Normalize(string name)
            => string.Concat((name ?? string.Empty)
                .Split(_delimiter, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.ToUpperInvariant()));
    }
}
