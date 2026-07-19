namespace OctoMap
{
    /// <summary>
    /// Describes a configured map that can be inspected or eagerly compiled.
    /// </summary>
    public sealed class ConfiguredMapDescriptor
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConfiguredMapDescriptor"/> class.
        /// </summary>
        /// <param name="sourceTypes">The source types.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <param name="isImplicit">A value indicating whether the map is implicit.</param>
        /// <param name="declarationSource">The map declaration source.</param>
        public ConfiguredMapDescriptor(
            IReadOnlyList<Type> sourceTypes,
            Type destinationType,
            bool isImplicit,
            string declarationSource)
        {
            SourceTypes = sourceTypes ?? throw new ArgumentNullException(nameof(sourceTypes));
            DestinationType = destinationType ?? throw new ArgumentNullException(nameof(destinationType));
            IsImplicit = isImplicit;
            DeclarationSource = declarationSource ?? "Unknown";
        }

        /// <summary>
        /// Gets the source types.
        /// </summary>
        public IReadOnlyList<Type> SourceTypes { get; }

        /// <summary>
        /// Gets the destination type.
        /// </summary>
        public Type DestinationType { get; }

        /// <summary>
        /// Gets whether this map was created implicitly at runtime.
        /// </summary>
        public bool IsImplicit { get; }

        /// <summary>
        /// Gets whether this descriptor represents a multi-source map.
        /// </summary>
        public bool IsMultiSource => SourceTypes.Count > 1;

        /// <summary>
        /// Gets the map declaration source.
        /// </summary>
        public string DeclarationSource { get; }
    }
}
