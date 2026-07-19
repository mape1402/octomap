namespace OctoMap.Configuration
{
    /// <summary>
    /// Identifies one or more source types and one destination type.
    /// </summary>
    internal readonly struct MapKey : IEquatable<MapKey>
    {
        private readonly Type[] _sourceTypes;
        private readonly int _sourceCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="MapKey"/> struct.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destinationType">The destination type.</param>
        public MapKey(Type sourceType, Type destinationType)
        {
            SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
            DestinationType = destinationType ?? throw new ArgumentNullException(nameof(destinationType));
            _sourceTypes = null;
            _sourceCount = 1;
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

            DestinationType = destinationType ?? throw new ArgumentNullException(nameof(destinationType));
            SourceType = sourceTypes[0] ?? throw new ArgumentException("Source types cannot contain null values.", nameof(sourceTypes));
            _sourceCount = sourceTypes.Count;

            if (sourceTypes.Count == 1)
            {
                _sourceTypes = null;
                return;
            }

            _sourceTypes = new Type[sourceTypes.Count];
            _sourceTypes[0] = SourceType;
            for (var index = 1; index < sourceTypes.Count; index++)
            {
                _sourceTypes[index] = sourceTypes[index] ?? throw new ArgumentException("Source types cannot contain null values.", nameof(sourceTypes));
            }
        }

        /// <summary>
        /// Gets the source type.
        /// </summary>
        public Type SourceType { get; }

        /// <summary>
        /// Gets the source types.
        /// </summary>
        public IReadOnlyList<Type> SourceTypes => _sourceTypes ?? new[] { SourceType };

        /// <summary>
        /// Gets the destination type.
        /// </summary>
        public Type DestinationType { get; }

        /// <inheritdoc/>
        public bool Equals(MapKey other)
        {
            if (DestinationType != other.DestinationType || _sourceCount != other._sourceCount)
            {
                return false;
            }

            if (_sourceCount == 1)
            {
                return SourceType == other.SourceType;
            }

            for (var index = 0; index < _sourceCount; index++)
            {
                if (_sourceTypes[index] != other._sourceTypes[index])
                {
                    return false;
                }
            }

            return true;
        }

        /// <inheritdoc/>
        public override bool Equals(object obj)
            => obj is MapKey other && Equals(other);

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            var hash = new HashCode();
            if (_sourceCount == 1)
            {
                hash.Add(SourceType);
            }
            else
            {
                for (var index = 0; index < _sourceCount; index++)
                {
                    hash.Add(_sourceTypes[index]);
                }
            }

            hash.Add(DestinationType);
            return hash.ToHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
            => $"{string.Join(", ", SourceTypes.Select(x => x.FullName))}->{DestinationType.FullName}";
    }
}
