namespace OctoMap.Configuration
{
    using System.Linq.Expressions;

    /// <summary>
    /// Represents an immutable configured type map.
    /// </summary>
    internal sealed class TypeMap : ITypeMap
    {
        private readonly Dictionary<string, MemberMap> _memberMaps = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<LifecycleActionMap> _lifecycleActions = new();

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeMap"/> class.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <param name="isImplicit">A value indicating whether the map is implicit.</param>
        public TypeMap(Type sourceType, Type destinationType, bool isImplicit)
            : this(sourceType, destinationType, isImplicit, new OctoMapOptions(), new MapDeclaration(isImplicit ? "Runtime implicit map" : "Explicit map"))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeMap"/> class.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <param name="isImplicit">A value indicating whether the map is implicit.</param>
        /// <param name="options">The map options snapshot.</param>
        /// <param name="declaration">The map declaration.</param>
        public TypeMap(
            Type sourceType,
            Type destinationType,
            bool isImplicit,
            OctoMapOptions options,
            MapDeclaration declaration)
        {
            SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
            DestinationType = destinationType ?? throw new ArgumentNullException(nameof(destinationType));
            IsImplicit = isImplicit;
            Options = options?.Clone() ?? throw new ArgumentNullException(nameof(options));
            Declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
        }

        /// <inheritdoc/>
        public Type SourceType { get; }

        /// <inheritdoc/>
        public Type DestinationType { get; }

        /// <inheritdoc/>
        public bool IsImplicit { get; }

        /// <summary>
        /// Gets the map options snapshot.
        /// </summary>
        public OctoMapOptions Options { get; }

        /// <summary>
        /// Gets the map declaration.
        /// </summary>
        public MapDeclaration Declaration { get; private set; }

        /// <summary>
        /// Sets the map declaration.
        /// </summary>
        /// <param name="declaration">The map declaration.</param>
        public void SetDeclaration(MapDeclaration declaration)
        {
            Declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
        }

        /// <summary>
        /// Gets the explicitly configured member maps.
        /// </summary>
        public IReadOnlyDictionary<string, MemberMap> MemberMaps => _memberMaps;

        /// <summary>
        /// Gets the configured lifecycle actions.
        /// </summary>
        public IReadOnlyList<LifecycleActionMap> LifecycleActions => _lifecycleActions;

        /// <summary>
        /// Gets or sets the configured destination construction expression.
        /// </summary>
        public LambdaExpression ConstructionExpression { get; set; }

        /// <summary>
        /// Adds a lifecycle action to this map.
        /// </summary>
        /// <param name="lifecycleAction">The lifecycle action.</param>
        public void AddLifecycleAction(LifecycleActionMap lifecycleAction)
        {
            if (lifecycleAction == null)
            {
                throw new ArgumentNullException(nameof(lifecycleAction));
            }

            _lifecycleActions.Add(lifecycleAction);
        }

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

        /// <summary>
        /// Gets or adds a member map for the specified destination property path.
        /// </summary>
        /// <param name="destinationPath">The destination property path.</param>
        /// <returns>The member map.</returns>
        public MemberMap GetOrAddMemberPathMap(IReadOnlyList<System.Reflection.PropertyInfo> destinationPath)
        {
            if (destinationPath == null)
            {
                throw new ArgumentNullException(nameof(destinationPath));
            }

            if (destinationPath.Count == 0)
            {
                throw new ArgumentException("A destination path must contain at least one property.", nameof(destinationPath));
            }

            var key = string.Join(".", destinationPath.Select(x => x.Name));
            if (!_memberMaps.TryGetValue(key, out var memberMap))
            {
                memberMap = new MemberMap(destinationPath[^1], destinationPath);
                _memberMaps[key] = memberMap;
            }

            return memberMap;
        }

        /// <summary>
        /// Copies a configured member map into this map.
        /// </summary>
        /// <param name="sourceMemberMap">The source member map.</param>
        public void CopyMemberMap(MemberMap sourceMemberMap)
        {
            if (sourceMemberMap == null)
            {
                throw new ArgumentNullException(nameof(sourceMemberMap));
            }

            var target = sourceMemberMap.UsesDestinationPath
                ? GetOrAddMemberPathMap(sourceMemberMap.DestinationPath)
                : GetOrAddMemberMap(sourceMemberMap.DestinationProperty);
            target.CopyFrom(sourceMemberMap);
        }
    }
}
