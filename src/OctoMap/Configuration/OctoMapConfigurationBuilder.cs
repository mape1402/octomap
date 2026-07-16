namespace OctoMap.Configuration
{
    /// <summary>
    /// Builds OctoMap configuration from profile declarations.
    /// </summary>
    internal sealed class OctoMapConfigurationBuilder : IOctoMapConfigurationBuilder
    {
        private readonly Dictionary<MapKey, TypeMap> _maps = new();

        /// <inheritdoc/>
        public IMapExpression<TSource, TDestination> CreateMap<TSource, TDestination>()
        {
            var map = new TypeMap(typeof(TSource), typeof(TDestination), false);
            _maps[new MapKey(map.SourceType, map.DestinationType)] = map;
            return new MapExpression<TSource, TDestination>();
        }

        /// <summary>
        /// Builds immutable configuration.
        /// </summary>
        /// <returns>The immutable configuration.</returns>
        public IOctoMapConfiguration Build()
            => new OctoMapConfiguration(new Dictionary<MapKey, TypeMap>(_maps));
    }
}
