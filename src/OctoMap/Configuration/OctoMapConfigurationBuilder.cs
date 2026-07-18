namespace OctoMap.Configuration
{
    using OctoMap.Validation;

    /// <summary>
    /// Builds OctoMap configuration from profile declarations.
    /// </summary>
    internal sealed class OctoMapConfigurationBuilder : IOctoMapConfigurationBuilder
    {
        private readonly Dictionary<MapKey, TypeMap> _maps = new();
        private readonly List<MultiSourceTypeMap> _multiMaps = new();

        /// <inheritdoc/>
        public IMapExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
        {
            var map = GetOrCreateMap(typeof(TSource), typeof(TDestination));
            return new MapExpression<TSource, TDestination>(map, this);
        }

        /// <inheritdoc/>
        public void CreateMap(Type sourceType, Type destinationType)
        {
            GetOrCreateMap(sourceType, destinationType);
        }

        /// <summary>
        /// Gets or creates a configured type map.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <returns>The configured type map.</returns>
        internal TypeMap GetOrCreateMap(Type sourceType, Type destinationType)
        {
            var key = new MapKey(sourceType, destinationType);
            if (!_maps.TryGetValue(key, out var map))
            {
                map = new TypeMap(sourceType, destinationType, false);
                _maps[key] = map;
            }

            return map;
        }

        /// <inheritdoc/>
        public IMultiMapExpression<TDestination> CreateMultiMap<TDestination>()
        {
            var map = new MultiSourceTypeMap(typeof(TDestination));
            _multiMaps.Add(map);
            return new MultiMapExpression<TDestination>(map);
        }

        /// <summary>
        /// Builds immutable configuration.
        /// </summary>
        /// <returns>The immutable configuration.</returns>
        public IOctoMapConfiguration Build()
        {
            var multiMaps = new Dictionary<MapKey, MultiSourceTypeMap>();
            foreach (var multiMap in _multiMaps)
            {
                multiMaps[new MapKey(multiMap.SourceTypes, multiMap.DestinationType)] = multiMap;
            }

            return new OctoMapConfiguration(
                new Dictionary<MapKey, TypeMap>(_maps),
                multiMaps,
                new OctoMapValidator());
        }
    }
}
