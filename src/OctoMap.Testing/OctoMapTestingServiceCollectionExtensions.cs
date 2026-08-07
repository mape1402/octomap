using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace OctoMap.Testing
{
    /// <summary>
    /// Registers OctoMap testing infrastructure in a dependency injection container.
    /// </summary>
    public static class OctoMapTestingServiceCollectionExtensions
    {
        /// <summary>
        /// Registers OctoMap and the testing mapper using maps discovered from the specified assemblies.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="mapAssemblies">The assemblies to scan for profiles, interfaces, and attributes.</param>
        /// <returns>The same service collection.</returns>
        public static IServiceCollection AddOctoMapTesting(this IServiceCollection services, params Assembly[] mapAssemblies)
            => services.AddOctoMapTesting(_ => { }, mapAssemblies);

        /// <summary>
        /// Registers OctoMap and the testing mapper using explicit OctoMap options.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">The OctoMap options callback.</param>
        /// <param name="mapAssemblies">The assemblies to scan for profiles, interfaces, and attributes.</param>
        /// <returns>The same service collection.</returns>
        public static IServiceCollection AddOctoMapTesting(
            this IServiceCollection services,
            Action<OctoMapOptions> configureOptions,
            params Assembly[] mapAssemblies)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            services.AddOctoMap(options =>
            {
                configureOptions?.Invoke(options);
            }, mapAssemblies ?? Array.Empty<Assembly>());

            RegisterTestingServices(services);
            return services;
        }

        /// <summary>
        /// Registers OctoMap testing services through the adapter-friendly contract.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="mapAssemblies">The assemblies to scan for profiles, interfaces, and attributes.</param>
        /// <returns>The same service collection.</returns>
        public static IServiceCollection AddOctoMapTestingAdapter(this IServiceCollection services, params Assembly[] mapAssemblies)
            => services.AddOctoMapTestingAdapter(_ => { }, mapAssemblies);

        /// <summary>
        /// Registers OctoMap testing services through the adapter-friendly contract using explicit OctoMap options.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configureOptions">The OctoMap options callback.</param>
        /// <param name="mapAssemblies">The assemblies to scan for profiles, interfaces, and attributes.</param>
        /// <returns>The same service collection.</returns>
        public static IServiceCollection AddOctoMapTestingAdapter(
            this IServiceCollection services,
            Action<OctoMapOptions> configureOptions,
            params Assembly[] mapAssemblies)
            => services.AddOctoMapTesting(configureOptions, mapAssemblies);

        private static void RegisterTestingServices(IServiceCollection services)
        {
            services.TryAddScoped<OctoMapTestingMapper>();
            services.TryAddScoped<IOctoMapTestingMapper>(sp => sp.GetRequiredService<OctoMapTestingMapper>());
            services.TryAddScoped<IOctoMapTestingAdapter>(sp => sp.GetRequiredService<OctoMapTestingMapper>());
        }
    }
}
