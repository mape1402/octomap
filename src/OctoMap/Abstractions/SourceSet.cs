namespace OctoMap
{
    /// <summary>
    /// Represents a runtime set of source instances used by a multi-source map.
    /// </summary>
    public sealed class SourceSet
    {
        private readonly List<object> _sources;

        private SourceSet(IEnumerable<object> sources)
        {
            _sources = sources?.Select(x => x ?? throw new ArgumentException("Source sets cannot contain null values.", nameof(sources))).ToList()
                ?? throw new ArgumentNullException(nameof(sources));

            if (_sources.Count == 0)
            {
                throw new ArgumentException("At least one source instance is required.", nameof(sources));
            }
        }

        /// <summary>
        /// Gets the source instances.
        /// </summary>
        public IReadOnlyList<object> Sources => _sources;

        /// <summary>
        /// Gets the runtime source types.
        /// </summary>
        public IReadOnlyList<Type> SourceTypes => _sources.Select(x => x.GetType()).ToArray();

        /// <summary>
        /// Creates a source set from the specified source instances.
        /// </summary>
        /// <param name="sources">The source instances.</param>
        /// <returns>The source set.</returns>
        public static SourceSet Of(params object[] sources)
            => new(sources);

        /// <summary>
        /// Gets the first source instance assignable to the requested type.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <returns>The matching source instance.</returns>
        public TSource Get<TSource>()
        {
            TSource match = default;
            var found = false;
            foreach (var source in _sources)
            {
                if (source is TSource typedSource)
                {
                    if (found)
                    {
                        throw new InvalidOperationException($"Source set contains more than one source assignable to '{typeof(TSource).FullName}'.");
                    }

                    match = typedSource;
                    found = true;
                }
            }

            if (found)
            {
                return match;
            }

            throw new InvalidOperationException($"Source set does not contain a source assignable to '{typeof(TSource).FullName}'.");
        }
    }
}
