namespace OctoMap.Configuration
{
    using System.Reflection;

    /// <summary>
    /// Represents an explicitly configured multi-source type map.
    /// </summary>
    internal sealed class MultiSourceTypeMap : ITypeMap
    {
        private readonly List<TypeMap> _sourceMaps = new();
        private readonly Dictionary<string, MultiSourceMemberMap> _contextMemberMaps = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiSourceTypeMap"/> class.
        /// </summary>
        /// <param name="destinationType">The destination type.</param>
        public MultiSourceTypeMap(Type destinationType)
        {
            DestinationType = destinationType ?? throw new ArgumentNullException(nameof(destinationType));
        }

        /// <inheritdoc/>
        public Type SourceType => SourceTypes.Count > 0 ? SourceTypes[0] : typeof(void);

        /// <summary>
        /// Gets the source types.
        /// </summary>
        public IReadOnlyList<Type> SourceTypes => _sourceMaps.Select(x => x.SourceType).ToArray();

        /// <inheritdoc/>
        public Type DestinationType { get; }

        /// <inheritdoc/>
        public bool IsImplicit => false;

        /// <summary>
        /// Gets the source-local type maps.
        /// </summary>
        public IReadOnlyList<TypeMap> SourceMaps => _sourceMaps;

        /// <summary>
        /// Gets the context member maps.
        /// </summary>
        public IReadOnlyDictionary<string, MultiSourceMemberMap> ContextMemberMaps => _contextMemberMaps;

        /// <summary>
        /// Adds a source-local map.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <returns>The source-local type map.</returns>
        public TypeMap AddSource(Type sourceType)
        {
            if (_sourceMaps.Any(x => x.SourceType == sourceType))
            {
                throw new InvalidOperationException($"Source type '{sourceType.FullName}' is already registered for destination type '{DestinationType.FullName}'.");
            }

            var sourceMap = new TypeMap(sourceType, DestinationType, false);
            _sourceMaps.Add(sourceMap);
            return sourceMap;
        }

        /// <summary>
        /// Gets or creates context configuration for a destination member.
        /// </summary>
        /// <param name="destinationProperty">The destination property.</param>
        /// <returns>The member map.</returns>
        public MultiSourceMemberMap GetOrAddContextMemberMap(PropertyInfo destinationProperty)
        {
            if (!_contextMemberMaps.TryGetValue(destinationProperty.Name, out var memberMap))
            {
                memberMap = new MultiSourceMemberMap(destinationProperty);
                _contextMemberMaps[destinationProperty.Name] = memberMap;
            }

            return memberMap;
        }
    }
}
