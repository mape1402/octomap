namespace OctoMap
{
    using OctoMap.Naming;

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

        /// <summary>
        /// Gets or sets the source member naming convention.
        /// </summary>
        public INamingConvention SourceNamingConvention { get; set; } = ExactNamingConvention.Instance;

        /// <summary>
        /// Gets or sets the destination member naming convention.
        /// </summary>
        public INamingConvention DestinationNamingConvention { get; set; } = ExactNamingConvention.Instance;

        /// <summary>
        /// Gets the source member prefixes removed before naming convention normalization.
        /// </summary>
        public IList<string> SourceMemberPrefixes { get; } = new List<string>();

        /// <summary>
        /// Gets the source member suffixes removed before naming convention normalization.
        /// </summary>
        public IList<string> SourceMemberSuffixes { get; } = new List<string>();

        /// <summary>
        /// Gets the destination member prefixes removed before naming convention normalization.
        /// </summary>
        public IList<string> DestinationMemberPrefixes { get; } = new List<string>();

        /// <summary>
        /// Gets the destination member suffixes removed before naming convention normalization.
        /// </summary>
        public IList<string> DestinationMemberSuffixes { get; } = new List<string>();
    }
}
