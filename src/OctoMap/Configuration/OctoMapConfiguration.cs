namespace OctoMap.Configuration
{
    using OctoMap.Validation;

    /// <summary>
    /// Represents immutable OctoMap configuration.
    /// </summary>
    internal sealed class OctoMapConfiguration : IOctoMapConfiguration
    {
        private readonly IReadOnlyDictionary<MapKey, TypeMap> _maps;
        private readonly IReadOnlyDictionary<MapKey, MultiSourceTypeMap> _multiMaps;
        private readonly IOctoMapValidator _validator;

        /// <summary>
        /// Initializes a new instance of the <see cref="OctoMapConfiguration"/> class.
        /// </summary>
        /// <param name="maps">The configured maps.</param>
        /// <param name="multiMaps">The configured multi-source maps.</param>
        /// <param name="validator">The configuration validator.</param>
        public OctoMapConfiguration(
            IReadOnlyDictionary<MapKey, TypeMap> maps,
            IReadOnlyDictionary<MapKey, MultiSourceTypeMap> multiMaps,
            IOctoMapValidator validator)
        {
            _maps = maps ?? throw new ArgumentNullException(nameof(maps));
            _multiMaps = multiMaps ?? throw new ArgumentNullException(nameof(multiMaps));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        /// <inheritdoc/>
        public ITypeMap FindMap(Type sourceType, Type destinationType)
        {
            var key = new MapKey(sourceType, destinationType);
            return _maps.TryGetValue(key, out var map) ? map : null;
        }

        /// <inheritdoc/>
        public ITypeMap FindMap(IReadOnlyList<Type> sourceTypes, Type destinationType)
        {
            var key = new MapKey(sourceTypes, destinationType);
            return _multiMaps.TryGetValue(key, out var map) ? map : null;
        }

        /// <inheritdoc/>
        public OctoMapValidationReport Validate()
            => _validator.Validate(_maps.Values.Cast<ITypeMap>().Concat(_multiMaps.Values).ToArray());

        /// <inheritdoc/>
        public void AssertValid()
        {
            var report = Validate();
            if (!report.IsValid)
            {
                throw new OctoMapValidationException(report);
            }
        }
    }
}
