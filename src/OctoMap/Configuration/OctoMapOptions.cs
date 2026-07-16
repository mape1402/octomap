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
    }
}
