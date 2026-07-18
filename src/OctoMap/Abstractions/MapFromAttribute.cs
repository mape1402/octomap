namespace OctoMap
{
    /// <summary>
    /// Registers a map from the specified source type to the annotated destination type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class MapFromAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MapFromAttribute"/> class.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        public MapFromAttribute(Type sourceType)
        {
            SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
        }

        /// <summary>
        /// Gets the source type.
        /// </summary>
        public Type SourceType { get; }
    }
}
