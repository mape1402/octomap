namespace OctoMap.Configuration
{
    /// <summary>
    /// Identifies a source and destination type pair.
    /// </summary>
    internal readonly struct MapKey : IEquatable<MapKey>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MapKey"/> struct.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destinationType">The destination type.</param>
        public MapKey(Type sourceType, Type destinationType)
        {
            SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
            DestinationType = destinationType ?? throw new ArgumentNullException(nameof(destinationType));
        }

        /// <summary>
        /// Gets the source type.
        /// </summary>
        public Type SourceType { get; }

        /// <summary>
        /// Gets the destination type.
        /// </summary>
        public Type DestinationType { get; }

        /// <inheritdoc/>
        public bool Equals(MapKey other)
            => SourceType == other.SourceType && DestinationType == other.DestinationType;

        /// <inheritdoc/>
        public override bool Equals(object obj)
            => obj is MapKey other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode()
            => HashCode.Combine(SourceType, DestinationType);

        /// <inheritdoc/>
        public override string ToString()
            => $"{SourceType.FullName}->{DestinationType.FullName}";
    }
}
