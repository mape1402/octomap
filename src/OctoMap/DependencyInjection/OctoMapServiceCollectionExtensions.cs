using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using OctoMap.Configuration;
using OctoMap.DependencyInjection;
using OctoMap.Generation;
using OctoMap.Generation.Dynabee;
using OctoMap.Planning;
using OctoMap.Runtime;

namespace OctoMap
{
    /// <summary>
    /// Registers OctoMap infrastructure in a dependency injection container.
    /// </summary>
    public static class OctoMapServiceCollectionExtensions
    {
        /// <summary>
        /// Registers OctoMap using profiles discovered from the specified assemblies.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="profileAssemblies">The assemblies to scan for profiles.</param>
        /// <returns>The same service collection.</returns>
        public static IServiceCollection AddOctoMap(this IServiceCollection services, params Assembly[] profileAssemblies)
            => services.AddOctoMap(_ => { }, profileAssemblies);

        /// <summary>
        /// Registers OctoMap using options and profiles discovered from the specified assemblies.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">The options configuration callback.</param>
        /// <param name="profileAssemblies">The assemblies to scan for profiles.</param>
        /// <returns>The same service collection.</returns>
        public static IServiceCollection AddOctoMap(
            this IServiceCollection services,
            Action<OctoMapOptions> configureOptions,
            params Assembly[] profileAssemblies)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var options = new OctoMapOptions();
            configureOptions?.Invoke(options);

            var discovery = new OctoMapProfileDiscovery();
            var configuration = BuildConfiguration(discovery.Discover(profileAssemblies));

            services.AddSingleton(options);
            services.AddSingleton(configuration);
            services.AddSingleton<IOctoMapConfiguration>(configuration);
            services.AddSingleton<IOctoMapProfileDiscovery, OctoMapProfileDiscovery>();
            services.AddSingleton<IMappingPlanBuilder, ConventionMappingPlanBuilder>();
            services.AddSingleton<IMappingGenerationBackend, DynabeeMappingGenerationBackend>();
            services.AddSingleton<ICompiledMapRegistry, CompiledMapRegistry>();
            services.AddSingleton<IMapContextFactory, MapContextFactory>();
            services.AddSingleton<IOctoMapper, OctoMapper>();
            services.AddTransient(typeof(IOctoMapper<,>), typeof(OctoMapper<,>));

            return services;
        }

        private static OctoMapConfiguration BuildConfiguration(IReadOnlyCollection<OctoMapProfile> profiles)
        {
            var builder = new OctoMapConfigurationBuilder();
            foreach (var profile in profiles)
            {
                profile.Configure(builder);
            }

            return (OctoMapConfiguration)builder.Build();
        }
    }
}
