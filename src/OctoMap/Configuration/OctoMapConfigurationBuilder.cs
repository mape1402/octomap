namespace OctoMap.Configuration
{
    using System.Linq.Expressions;
    using OctoMap.Diagnostics;
    using OctoMap.Planning;
    using OctoMap.Validation;

    /// <summary>
    /// Builds OctoMap configuration from profile declarations.
    /// </summary>
    internal sealed class OctoMapConfigurationBuilder : IOctoMapConfigurationBuilder
    {
        private readonly Dictionary<MapKey, TypeMap> _maps = new();
        private readonly Dictionary<MapKey, TypeConversionMap> _conversions = new();
        private readonly List<MultiSourceTypeMap> _multiMaps = new();
        private readonly OctoMapOptions _options;
        private readonly Dictionary<int, Action<object, object, IMapContext>> _inlineLifecycleActions = new();
        private int _inlineLifecycleActionId;

        /// <summary>
        /// Initializes a new instance of the <see cref="OctoMapConfigurationBuilder"/> class.
        /// </summary>
        /// <param name="options">The configuration options.</param>
        public OctoMapConfigurationBuilder(OctoMapOptions options = null)
        {
            _options = options ?? new OctoMapOptions();
        }

        /// <summary>
        /// Gets the configured single-source maps.
        /// </summary>
        internal IReadOnlyDictionary<MapKey, TypeMap> Maps => new Dictionary<MapKey, TypeMap>(_maps);

        /// <summary>
        /// Gets the configured multi-source maps.
        /// </summary>
        internal IReadOnlyDictionary<MapKey, MultiSourceTypeMap> MultiMaps => BuildMultiMaps();

        /// <summary>
        /// Gets the configured global conversions.
        /// </summary>
        internal ITypeConversionRegistry TypeConversions => new TypeConversionRegistry(_conversions.Values);

        /// <summary>
        /// Gets the configured inline lifecycle actions.
        /// </summary>
        internal IReadOnlyDictionary<int, Action<object, object, IMapContext>> InlineLifecycleActions
            => new Dictionary<int, Action<object, object, IMapContext>>(_inlineLifecycleActions);

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

        /// <inheritdoc/>
        public void CreateConverter<TSource, TDestination>(Expression<Func<TSource, TDestination>> conversionExpression)
        {
            if (conversionExpression == null)
            {
                throw new ArgumentNullException(nameof(conversionExpression));
            }

            AddConversion(new TypeConversionMap(typeof(TSource), typeof(TDestination), conversionExpression, null));
        }

        /// <inheritdoc/>
        public void CreateConverter<TConverter, TSource, TDestination>()
            where TConverter : IValueConverter<TSource, TDestination>
            => AddConversion(new TypeConversionMap(typeof(TSource), typeof(TDestination), null, typeof(TConverter)));

        /// <inheritdoc/>
        public void UseSourceNamingConvention(OctoMap.Naming.INamingConvention namingConvention)
            => _options.SourceNamingConvention = namingConvention ?? throw new ArgumentNullException(nameof(namingConvention));

        /// <inheritdoc/>
        public void UseDestinationNamingConvention(OctoMap.Naming.INamingConvention namingConvention)
            => _options.DestinationNamingConvention = namingConvention ?? throw new ArgumentNullException(nameof(namingConvention));

        /// <inheritdoc/>
        public void RecognizeSourcePrefixes(params string[] prefixes)
            => AddAffixes(_options.SourceMemberPrefixes, prefixes);

        /// <inheritdoc/>
        public void RecognizeSourceSuffixes(params string[] suffixes)
            => AddAffixes(_options.SourceMemberSuffixes, suffixes);

        /// <inheritdoc/>
        public void RecognizeDestinationPrefixes(params string[] prefixes)
            => AddAffixes(_options.DestinationMemberPrefixes, prefixes);

        /// <inheritdoc/>
        public void RecognizeDestinationSuffixes(params string[] suffixes)
            => AddAffixes(_options.DestinationMemberSuffixes, suffixes);

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

        /// <summary>
        /// Attempts to get a configured type map.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <param name="map">The configured type map.</param>
        /// <returns>True when the map exists.</returns>
        internal bool TryGetMap(Type sourceType, Type destinationType, out TypeMap map)
            => _maps.TryGetValue(new MapKey(sourceType, destinationType), out map);

        /// <summary>
        /// Adds an inline lifecycle action to the configuration.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="action">The lifecycle action.</param>
        /// <returns>The action identifier.</returns>
        internal int AddInlineLifecycleAction<TSource, TDestination>(Action<TSource, TDestination, IMapContext> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            var id = System.Threading.Interlocked.Increment(ref _inlineLifecycleActionId);
            _inlineLifecycleActions[id] = (source, destination, context) => action((TSource)source, (TDestination)destination, context);
            return id;
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
            => new OctoMapConfiguration(
                Maps,
                MultiMaps,
                new OctoMapValidator(TypeConversions, _options),
                new ConventionMappingPlanBuilder(_options, TypeConversions),
                new ConventionMappingPlanDescriber());

        private void AddConversion(TypeConversionMap conversion)
        {
            _conversions[new MapKey(conversion.SourceType, conversion.DestinationType)] = conversion;
        }

        private static void AddAffixes(ICollection<string> target, IEnumerable<string> values)
        {
            if (values == null)
            {
                return;
            }

            foreach (var value in values.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                target.Add(value);
            }
        }

        private IReadOnlyDictionary<MapKey, MultiSourceTypeMap> BuildMultiMaps()
        {
            var multiMaps = new Dictionary<MapKey, MultiSourceTypeMap>();
            foreach (var multiMap in _multiMaps)
            {
                multiMaps[new MapKey(multiMap.SourceTypes, multiMap.DestinationType)] = multiMap;
            }

            return multiMaps;
        }
    }
}
