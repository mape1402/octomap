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

        /// <summary>
        /// Gets or sets how duplicate explicit map declarations are handled.
        /// </summary>
        public DuplicateMapPolicy DuplicateMapPolicy { get; set; } = DuplicateMapPolicy.Merge;

        /// <summary>
        /// Creates a copy of the current options.
        /// </summary>
        /// <returns>The copied options.</returns>
        internal OctoMapOptions Clone()
        {
            var clone = new OctoMapOptions
            {
                EnableRuntimeImplicitMaps = EnableRuntimeImplicitMaps,
                AllowNullCollections = AllowNullCollections,
                IgnoreNullSourceValues = IgnoreNullSourceValues,
                SourceNamingConvention = SourceNamingConvention,
                DestinationNamingConvention = DestinationNamingConvention,
                DuplicateMapPolicy = DuplicateMapPolicy
            };

            Copy(SourceMemberPrefixes, clone.SourceMemberPrefixes);
            Copy(SourceMemberSuffixes, clone.SourceMemberSuffixes);
            Copy(DestinationMemberPrefixes, clone.DestinationMemberPrefixes);
            Copy(DestinationMemberSuffixes, clone.DestinationMemberSuffixes);
            return clone;
        }

        /// <summary>
        /// Copies all option values from another options instance.
        /// </summary>
        /// <param name="source">The source options.</param>
        internal void CopyFrom(OctoMapOptions source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            EnableRuntimeImplicitMaps = source.EnableRuntimeImplicitMaps;
            AllowNullCollections = source.AllowNullCollections;
            IgnoreNullSourceValues = source.IgnoreNullSourceValues;
            SourceNamingConvention = source.SourceNamingConvention;
            DestinationNamingConvention = source.DestinationNamingConvention;
            DuplicateMapPolicy = source.DuplicateMapPolicy;

            SourceMemberPrefixes.Clear();
            SourceMemberSuffixes.Clear();
            DestinationMemberPrefixes.Clear();
            DestinationMemberSuffixes.Clear();

            Copy(source.SourceMemberPrefixes, SourceMemberPrefixes);
            Copy(source.SourceMemberSuffixes, SourceMemberSuffixes);
            Copy(source.DestinationMemberPrefixes, DestinationMemberPrefixes);
            Copy(source.DestinationMemberSuffixes, DestinationMemberSuffixes);
        }

        private static void Copy(IEnumerable<string> source, ICollection<string> destination)
        {
            foreach (var value in source)
            {
                destination.Add(value);
            }
        }
    }
}
