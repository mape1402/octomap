namespace OctoMap.Configuration
{
    /// <summary>
    /// Represents an immutable configured type map.
    /// </summary>
    internal sealed class TypeMap : ITypeMap
    {
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
    }
}
