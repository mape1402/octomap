using System.Collections.Concurrent;
using OctoMap.Configuration;
using OctoMap.Generation;
using OctoMap.Planning;
using OctoMap.Validation;

namespace OctoMap.Runtime
{
    /// <summary>
    /// Provides cached compiled maps.
    /// </summary>
    internal sealed class CompiledMapRegistry : ICompiledMapRegistry
    {
        private readonly ConcurrentDictionary<MapKey, CompiledMap> _compiledMaps = new();
        private readonly IOctoMapConfiguration _configuration;
        private readonly OctoMapOptions _options;
        private readonly IMappingPlanBuilder _planBuilder;
        private readonly IMappingGenerationBackend _generationBackend;
        private readonly IOctoMapValidator _validator;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompiledMapRegistry"/> class.
        /// </summary>
        /// <param name="configuration">The map configuration.</param>
        /// <param name="options">The runtime options.</param>
        /// <param name="planBuilder">The mapping plan builder.</param>
        /// <param name="generationBackend">The generation backend.</param>
        /// <param name="validator">The configuration validator.</param>
        public CompiledMapRegistry(
            IOctoMapConfiguration configuration,
            OctoMapOptions options,
            IMappingPlanBuilder planBuilder,
            IMappingGenerationBackend generationBackend,
            IOctoMapValidator validator)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _planBuilder = planBuilder ?? throw new ArgumentNullException(nameof(planBuilder));
            _generationBackend = generationBackend ?? throw new ArgumentNullException(nameof(generationBackend));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        /// <inheritdoc/>
        public CompiledMap GetOrAdd(Type sourceType, Type destinationType)
        {
            var key = new MapKey(sourceType, destinationType);
            return _compiledMaps.GetOrAdd(key, _ => Compile(sourceType, destinationType));
        }

        private CompiledMap Compile(Type sourceType, Type destinationType)
        {
            var typeMap = _configuration.FindMap(sourceType, destinationType)
                ?? CreateImplicitMap(sourceType, destinationType);

            var report = _validator.Validate(new[] { typeMap });
            if (!report.IsValid)
            {
                throw new OctoMapValidationException(report);
            }

            var plan = _planBuilder.Build(typeMap);
            if (!_generationBackend.Supports(plan))
            {
                throw new InvalidOperationException($"Generation backend '{_generationBackend.Name}' does not support map '{sourceType.FullName}->{destinationType.FullName}'.");
            }

            return _generationBackend.Compile(plan);
        }

        private ITypeMap CreateImplicitMap(Type sourceType, Type destinationType)
        {
            if (!_options.EnableRuntimeImplicitMaps)
            {
                throw new InvalidOperationException($"Map '{sourceType.FullName}->{destinationType.FullName}' is not configured and runtime implicit maps are disabled.");
            }

            return new TypeMap(sourceType, destinationType, true);
        }
    }
}
