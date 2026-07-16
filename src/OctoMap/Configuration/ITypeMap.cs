namespace OctoMap
{
    /// <summary>
    /// Describes a configured source and destination type pair.
    /// </summary>
    public interface ITypeMap
    {
        /// <summary>
        /// Gets the source type.
        /// </summary>
        Type SourceType { get; }

        /// <summary>
        /// Gets the destination type.
        /// </summary>
        Type DestinationType { get; }

        /// <summary>
        /// Gets whether this map was created implicitly at runtime.
        /// </summary>
        bool IsImplicit { get; }
    }
}
