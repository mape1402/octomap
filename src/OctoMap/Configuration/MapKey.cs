namespace OctoMap.Configuration
{
    /// <summary>
    /// Identifies one or more source types and one destination type.
    /// </summary>
    internal readonly struct MapKey : IEquatable<MapKey>
    {
        private readonly Type[] _sourceTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="MapKey"/> struct.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destinationType">The destination type.</param>
        public MapKey(Type sourceType, Type destinationType)
            : this(new[] { sourceType }, destinationType)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapKey"/> struct.
        /// </summary>
        /// <param name="sourceTypes">The source types.</param>
        /// <param name="destinationType">The destination type.</param>
        public MapKey(IReadOnlyList<Type> sourceTypes, Type destinationType)
        {
            if (sourceTypes == null)
            {
                throw new ArgumentNullException(nameof(sourceTypes));
            }

            if (sourceTypes.Count == 0)
            {
                throw new ArgumentException("At least one source type is required.", nameof(sourceTypes));
            }

            _sourceTypes = sourceTypes.Select(x => x ?? throw new ArgumentException("Source types cannot contain null values.", nameof(sourceTypes))).ToArray();
            SourceType = _sourceTypes[0];
            DestinationType = destinationType ?? throw new ArgumentNullException(nameof(destinationType));
        }

        /// <summary>
        /// Gets the source type.
        /// </summary>
        public Type SourceType { get; }

        /// <summary>
        /// Gets the source types.
        /// </summary>
        public IReadOnlyList<Type> SourceTypes => _sourceTypes;

        /// <summary>
        /// Gets the destination type.
        /// </summary>
        public Type DestinationType { get; }

        /// <inheritdoc/>
        public bool Equals(MapKey other)
            => DestinationType == other.DestinationType && SourceTypes.SequenceEqual(other.SourceTypes);

        /// <inheritdoc/>
        public override bool Equals(object obj)
            => obj is MapKey other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            foreach (var sourceType in SourceTypes)
            {
                hash.Add(sourceType);
            }

            hash.Add(DestinationType);
            return hash.ToHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
            => $"{string.Join(", ", SourceTypes.Select(x => x.FullName))}->{DestinationType.FullName}";
    }
}
