namespace OctoMap
{
    /// <summary>
    /// Defines global OctoMap runtime options.
    /// </summary>
    public sealed class OctoMapOptions
    {
        /// <summary>
        /// Gets or sets whether OctoMap can create convention-based maps at runtime.
        /// </summary>
        public bool EnableRuntimeImplicitMaps { get; set; } = true;

        /// <summary>
        /// Gets or sets whether null source collections are mapped as null destination collections.
        /// </summary>
        public bool AllowNullCollections { get; set; } = true;

        /// <summary>
        /// Gets or sets whether null resolved source values skip destination assignment.
        /// </summary>
        public bool IgnoreNullSourceValues { get; set; }
    }
}
