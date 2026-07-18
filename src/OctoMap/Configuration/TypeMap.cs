namespace OctoMap.Configuration
{
    using System.Linq.Expressions;

    /// <summary>
    /// Represents an immutable configured type map.
    /// </summary>
    internal sealed class TypeMap : ITypeMap
    {
        private readonly Dictionary<string, MemberMap> _memberMaps = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeMap"/> class.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <param name="isImplicit">A value indicating whether the map is implicit.</param>
        public TypeMap(Type sourceType, Type destinationType, bool isImplicit)
        {
            SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
            DestinationType = destinationType ?? throw new ArgumentNullException(nameof(destinationType));
            IsImplicit = isImplicit;
        }

        /// <inheritdoc/>
        public Type SourceType { get; }

        /// <inheritdoc/>
        public Type DestinationType { get; }

        /// <inheritdoc/>
        public bool IsImplicit { get; }

        /// <summary>
        /// Gets the explicitly configured member maps.
        /// </summary>
        public IReadOnlyDictionary<string, MemberMap> MemberMaps => _memberMaps;

        /// <summary>
        /// Gets or sets the configured destination construction expression.
        /// </summary>
        public LambdaExpression ConstructionExpression { get; set; }

        /// <summary>
        /// Gets or creates explicit configuration for a destination member.
        /// </summary>
        /// <param name="destinationProperty">The destination property.</param>
        /// <returns>The member map.</returns>
        public MemberMap GetOrAddMemberMap(System.Reflection.PropertyInfo destinationProperty)
        {
            if (!_memberMaps.TryGetValue(destinationProperty.Name, out var memberMap))
            {
                memberMap = new MemberMap(destinationProperty);
                _memberMaps[destinationProperty.Name] = memberMap;
            }

            return memberMap;
        }
    }
}
