namespace OctoMap.Configuration
{
    using OctoMap.Planning;
    using OctoMap.Validation;

    /// <summary>
    /// Represents immutable OctoMap configuration.
    /// </summary>
    internal sealed class OctoMapConfiguration : IOctoMapConfiguration
    {
        private readonly IReadOnlyDictionary<MapKey, TypeMap> _maps;
        private readonly IReadOnlyDictionary<MapKey, MultiSourceTypeMap> _multiMaps;
        private readonly IReadOnlyList<string> _profiles;
        private readonly IOctoMapValidator _validator;
        private readonly IMappingPlanBuilder _planBuilder;
        private readonly IMappingPlanDescriber _planDescriber;

        /// <summary>
        /// Initializes a new instance of the <see cref="OctoMapConfiguration"/> class.
        /// </summary>
        /// <param name="maps">The configured maps.</param>
        /// <param name="multiMaps">The configured multi-source maps.</param>
        /// <param name="profiles">The loaded profile names.</param>
        /// <param name="validator">The configuration validator.</param>
        /// <param name="planBuilder">The mapping plan builder.</param>
        /// <param name="planDescriber">The mapping plan describer.</param>
        public OctoMapConfiguration(
            IReadOnlyDictionary<MapKey, TypeMap> maps,
            IReadOnlyDictionary<MapKey, MultiSourceTypeMap> multiMaps,
            IReadOnlyList<string> profiles,
            IOctoMapValidator validator,
            IMappingPlanBuilder planBuilder,
            IMappingPlanDescriber planDescriber)
        {
            _maps = maps ?? throw new ArgumentNullException(nameof(maps));
            _multiMaps = multiMaps ?? throw new ArgumentNullException(nameof(multiMaps));
            _profiles = profiles ?? throw new ArgumentNullException(nameof(profiles));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _planBuilder = planBuilder ?? throw new ArgumentNullException(nameof(planBuilder));
            _planDescriber = planDescriber ?? throw new ArgumentNullException(nameof(planDescriber));
        }

        /// <inheritdoc/>
        public ITypeMap FindMap(Type sourceType, Type destinationType)
        {
            var key = new MapKey(sourceType, destinationType);
            if (_maps.TryGetValue(key, out var map))
            {
                return map;
            }

            if (TryCreateClosedGenericMap(sourceType, destinationType, out var closedGenericMap))
            {
                return closedGenericMap;
            }

            return _maps.Values
                .Where(x => x.SourceType.IsAssignableFrom(sourceType)
                    && destinationType.IsAssignableFrom(x.DestinationType))
                .OrderBy(x => GetInheritanceDistance(sourceType, x.SourceType))
                .ThenBy(x => GetInheritanceDistance(x.DestinationType, destinationType))
                .FirstOrDefault();
        }

        /// <inheritdoc/>
        public ITypeMap FindMap(IReadOnlyList<Type> sourceTypes, Type destinationType)
        {
            var key = new MapKey(sourceTypes, destinationType);
            return _multiMaps.TryGetValue(key, out var map) ? map : null;
        }

        /// <inheritdoc/>
        public MappingPlan GetPlan<TSource, TDestination>()
            => GetPlan(typeof(TSource), typeof(TDestination));

        /// <inheritdoc/>
        public MappingPlan GetPlan(Type sourceType, Type destinationType)
        {
            var map = FindMap(sourceType, destinationType);
            if (map == null)
            {
                throw new InvalidOperationException($"Map '{sourceType.FullName}->{destinationType.FullName}' is not configured.");
            }

            ValidateOrThrow(map);
            return _planBuilder.Build(map);
        }

        /// <inheritdoc/>
        public MappingPlan GetPlan(IReadOnlyList<Type> sourceTypes, Type destinationType)
        {
            var map = FindMap(sourceTypes, destinationType);
            if (map == null)
            {
                throw new InvalidOperationException($"Map '{string.Join(",", sourceTypes.Select(x => x.FullName))}->{destinationType.FullName}' is not configured.");
            }

            ValidateOrThrow(map);
            return _planBuilder.Build(map);
        }

        /// <inheritdoc/>
        public string DescribeMap<TSource, TDestination>()
            => DescribeMap(typeof(TSource), typeof(TDestination));

        /// <inheritdoc/>
        public string DescribeMap(Type sourceType, Type destinationType)
            => _planDescriber.Describe(GetPlan(sourceType, destinationType));

        /// <inheritdoc/>
        public string DescribeMap(IReadOnlyList<Type> sourceTypes, Type destinationType)
            => _planDescriber.Describe(GetPlan(sourceTypes, destinationType));

        /// <inheritdoc/>
        public IReadOnlyList<string> GetProfiles()
            => _profiles;

        /// <inheritdoc/>
        public string DescribeConfiguration()
        {
            var lines = new List<string>
            {
                "Profiles:"
            };

            if (_profiles.Count == 0)
            {
                lines.Add("- none");
            }
            else
            {
                lines.AddRange(_profiles.OrderBy(x => x).Select(x => $"- {x}"));
            }

            lines.Add("Maps:");
            if (_maps.Count == 0)
            {
                lines.Add("- none");
            }
            else
            {
                lines.AddRange(_maps.Values
                    .OrderBy(x => x.SourceType.FullName)
                    .ThenBy(x => x.DestinationType.FullName)
                    .Select(x => $"- {FormatType(x.SourceType)} -> {FormatType(x.DestinationType)} ({x.Declaration.Source})"));
            }

            lines.Add("Multi-source maps:");
            if (_multiMaps.Count == 0)
            {
                lines.Add("- none");
            }
            else
            {
                lines.AddRange(_multiMaps.Values
                    .OrderBy(x => x.DestinationType.FullName)
                    .Select(x => $"- {string.Join(", ", x.SourceTypes.Select(FormatType))} -> {FormatType(x.DestinationType)}"));
            }

            return string.Join(Environment.NewLine, lines);
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

        private void ValidateOrThrow(ITypeMap map)
        {
            var report = _validator.Validate(new[] { map });
            if (!report.IsValid)
            {
                throw new OctoMapValidationException(report);
            }
        }

        private static int GetInheritanceDistance(Type type, Type candidateBaseType)
        {
            if (type == candidateBaseType)
            {
                return 0;
            }

            if (candidateBaseType.IsInterface)
            {
                return type.GetInterfaces().Contains(candidateBaseType) ? 1 : int.MaxValue;
            }

            var distance = 0;
            var current = type;
            while (current != null)
            {
                if (current == candidateBaseType)
                {
                    return distance;
                }

                distance++;
                current = current.BaseType;
            }

            return int.MaxValue;
        }

        private bool TryCreateClosedGenericMap(Type sourceType, Type destinationType, out ITypeMap map)
        {
            map = null;
            if (!sourceType.IsGenericType || !destinationType.IsGenericType)
            {
                return false;
            }

            var sourceDefinition = sourceType.GetGenericTypeDefinition();
            var destinationDefinition = destinationType.GetGenericTypeDefinition();
            var openMap = _maps.Values.FirstOrDefault(x =>
                x.SourceType.IsGenericTypeDefinition
                && x.DestinationType.IsGenericTypeDefinition
                && x.SourceType == sourceDefinition
                && x.DestinationType == destinationDefinition);
            if (openMap == null)
            {
                return false;
            }

            map = new TypeMap(sourceType, destinationType, false, openMap.Options, new MapDeclaration($"Closed generic map from {openMap.Declaration.Source}"));
            return true;
        }

        private static string FormatType(Type type)
            => type.FullName ?? type.Name;
    }
}
