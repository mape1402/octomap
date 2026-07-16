namespace OctoMap.Configuration
{
    using OctoMap.Validation;

    /// <summary>
    /// Represents immutable OctoMap configuration.
    /// </summary>
    internal sealed class OctoMapConfiguration : IOctoMapConfiguration
    {
        private readonly IReadOnlyDictionary<MapKey, TypeMap> _maps;
        private readonly IOctoMapValidator _validator;

        /// <summary>
        /// Initializes a new instance of the <see cref="OctoMapConfiguration"/> class.
        /// </summary>
        /// <param name="maps">The configured maps.</param>
        /// <param name="validator">The configuration validator.</param>
        public OctoMapConfiguration(IReadOnlyDictionary<MapKey, TypeMap> maps, IOctoMapValidator validator)
        {
            _maps = maps ?? throw new ArgumentNullException(nameof(maps));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        /// <inheritdoc/>
        public ITypeMap FindMap(Type sourceType, Type destinationType)
        {
            var key = new MapKey(sourceType, destinationType);
            return _maps.TryGetValue(key, out var map) ? map : null;
        }

        /// <inheritdoc/>
        public OctoMapValidationReport Validate()
            => _validator.Validate(_maps.Values.Cast<ITypeMap>().ToArray());

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
