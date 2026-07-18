using System.Reflection;
using DynaBee.FluentApi.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OctoMap.Configuration;
using OctoMap.DependencyInjection;
using OctoMap.Generation;
using OctoMap.Generation.Dynabee;
using OctoMap.Planning;
using OctoMap.Projection;
using OctoMap.Runtime;
using OctoMap.Validation;

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
            var configuration = BuildConfiguration(discovery.Discover(profileAssemblies), profileAssemblies);

            services.AddSingleton(options);
            services.AddSingleton(configuration);
            services.AddSingleton<IOctoMapConfiguration>(configuration);
            services.AddSingleton<IOctoMapProfileDiscovery, OctoMapProfileDiscovery>();
            services.AddSingleton<IOctoMapValidator, OctoMapValidator>();
            services.AddSingleton<IMappingPlanBuilder, ConventionMappingPlanBuilder>();
            services.TryAddSingleton<IDynaBeeAssemblyBuilderFactory, DynaBeeAssemblyBuilderFactory>();
            services.AddSingleton<IMappingGenerationBackend, DynabeeMappingGenerationBackend>();
            services.AddSingleton<ICompiledMapRegistry, CompiledMapRegistry>();
            services.AddSingleton<IOctoProjectionBuilder, OctoProjectionBuilder>();
            services.AddScoped<IMapContextFactory, MapContextFactory>();
            services.AddScoped<IOctoMapper, OctoMapper>();
            services.AddTransient(typeof(IOctoMapper<,>), typeof(OctoMapper<,>));

            return services;
        }

        private static OctoMapConfiguration BuildConfiguration(IReadOnlyCollection<OctoMapProfile> profiles, IReadOnlyCollection<Assembly> profileAssemblies)
        {
            var builder = new OctoMapConfigurationBuilder();
            foreach (var profile in profiles)
            {
                profile.Configure(builder);
            }

            foreach (var assembly in profileAssemblies ?? Array.Empty<Assembly>())
            {
                RegisterInterfaceMaps(builder, assembly);
            }

            return (OctoMapConfiguration)builder.Build();
        }

        private static void RegisterInterfaceMaps(OctoMapConfigurationBuilder builder, Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface)
                {
                    continue;
                }

                foreach (var interfaceType in type.GetInterfaces())
                {
                    if (!interfaceType.IsGenericType)
                    {
                        continue;
                    }

                    var genericDefinition = interfaceType.GetGenericTypeDefinition();
                    var relatedType = interfaceType.GetGenericArguments()[0];

                    if (genericDefinition == typeof(IMapFrom<>))
                    {
                        builder.CreateMap(relatedType, type);
                    }

                    if (genericDefinition == typeof(IMapTo<>))
                    {
                        builder.CreateMap(type, relatedType);
                    }
                }
            }
        }
    }
}
