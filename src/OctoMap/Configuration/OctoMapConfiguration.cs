namespace OctoMap.Configuration
{
    /// <summary>
    /// Represents immutable OctoMap configuration.
    /// </summary>
    internal sealed class OctoMapConfiguration : IOctoMapConfiguration
    {
        private readonly IReadOnlyDictionary<MapKey, TypeMap> _maps;

        /// <summary>
        /// Initializes a new instance of the <see cref="OctoMapConfiguration"/> class.
        /// </summary>
        /// <param name="maps">The configured maps.</param>
        public OctoMapConfiguration(IReadOnlyDictionary<MapKey, TypeMap> maps)
        {
            _maps = maps ?? throw new ArgumentNullException(nameof(maps));
        }

        /// <inheritdoc/>
        public ITypeMap FindMap(Type sourceType, Type destinationType)
        {
            var key = new MapKey(sourceType, destinationType);
            return _maps.TryGetValue(key, out var map) ? map : null;
        }
    }
}
