namespace OctoMap.Configuration
{
    /// <summary>
    /// Describes where a map was declared.
    /// </summary>
    internal sealed class MapDeclaration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MapDeclaration"/> class.
        /// </summary>
        /// <param name="source">The declaration source.</param>
        public MapDeclaration(string source)
        {
            Source = source ?? "Unknown";
        }

        /// <summary>
        /// Gets the declaration source.
        /// </summary>
        public string Source { get; }
    }
}
