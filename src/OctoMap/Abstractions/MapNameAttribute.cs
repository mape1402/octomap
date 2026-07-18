namespace OctoMap
{
    /// <summary>
    /// Maps the annotated destination member from a source member with a different name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class MapNameAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MapNameAttribute"/> class.
        /// </summary>
        /// <param name="name">The source member name.</param>
        public MapNameAttribute(string name)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
        }

        /// <summary>
        /// Gets the source member name.
        /// </summary>
        public string Name { get; }
    }
}
