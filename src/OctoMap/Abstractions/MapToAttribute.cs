namespace OctoMap
{
    /// <summary>
    /// Registers a map from the annotated source type to the specified destination type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public sealed class MapToAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MapToAttribute"/> class.
        /// </summary>
        /// <param name="destinationType">The destination type.</param>
        public MapToAttribute(Type destinationType)
        {
            DestinationType = destinationType ?? throw new ArgumentNullException(nameof(destinationType));
        }

        /// <summary>
        /// Gets the destination type.
        /// </summary>
        public Type DestinationType { get; }
    }
}
