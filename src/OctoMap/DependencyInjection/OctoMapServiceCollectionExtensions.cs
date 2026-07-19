using System.Reflection;
using System.Linq.Expressions;
using DynaBee.FluentApi.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OctoMap.Configuration;
using OctoMap.DependencyInjection;
using OctoMap.Diagnostics;
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
        /// Registers OctoMap using explicit registration configuration.
        /// </summary>
        /// <param name="services">The service collection.</param>
        /// <param name="configure">The registration configuration callback.</param>
        /// <returns>The same service collection.</returns>
        public static IServiceCollection AddOctoMap(
            this IServiceCollection services,
            Action<IOctoMapRegistrationBuilder> configure)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var registrationBuilder = new OctoMapRegistrationBuilder();
            configure(registrationBuilder);
            return services.AddOctoMap(registrationBuilder);
        }

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

            var registrationBuilder = new OctoMapRegistrationBuilder();
            configureOptions?.Invoke(registrationBuilder.Options);
            foreach (var assembly in profileAssemblies ?? Array.Empty<Assembly>())
            {
                registrationBuilder.AddMaps(assembly);
            }

            return services.AddOctoMap(registrationBuilder);
        }

        private static IServiceCollection AddOctoMap(
            this IServiceCollection services,
            OctoMapRegistrationBuilder registrationBuilder)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            var options = registrationBuilder.Options;

            var discovery = new OctoMapProfileDiscovery();
            var discoveredProfiles = discovery
                .Discover(registrationBuilder.ProfileAssemblies.ToArray())
                .Where(x => registrationBuilder.AllowsProfile(x.GetType()))
                .ToArray();
            var profiles = registrationBuilder.Profiles.Concat(discoveredProfiles).ToArray();
            var configurationBuilder = BuildConfigurationBuilder(options, profiles, registrationBuilder.MapAssemblies, registrationBuilder);
            var maps = configurationBuilder.Maps;
            var multiMaps = configurationBuilder.MultiMaps;
            var typeConversions = configurationBuilder.TypeConversions;
            var inlineLifecycleActions = configurationBuilder.InlineLifecycleActions;

            services.AddSingleton(options);
            services.AddSingleton<IOctoMapProfileDiscovery, OctoMapProfileDiscovery>();
            services.AddSingleton(typeConversions);
            services.AddSingleton<IOctoMapValidator>(sp => new OctoMapValidator(
                sp.GetRequiredService<ITypeConversionRegistry>(),
                sp.GetRequiredService<OctoMapOptions>()));
            services.AddSingleton<IMappingPlanBuilder, ConventionMappingPlanBuilder>();
            services.TryAddSingleton<IMappingPlanDescriber, ConventionMappingPlanDescriber>();
            services.AddSingleton(sp => new OctoMapConfiguration(
                maps,
                multiMaps,
                profiles.Select(x => x.GetType().FullName ?? x.GetType().Name).ToArray(),
                sp.GetRequiredService<IOctoMapValidator>(),
                sp.GetRequiredService<IMappingPlanBuilder>(),
                sp.GetRequiredService<IMappingPlanDescriber>()));
            services.AddSingleton<IOctoMapConfiguration>(sp => sp.GetRequiredService<OctoMapConfiguration>());
            services.TryAddSingleton<IDynaBeeAssemblyBuilderFactory, DynaBeeAssemblyBuilderFactory>();
            services.AddSingleton<IInlineLifecycleActionRegistry>(new InlineLifecycleActionRegistry(inlineLifecycleActions));
            services.AddSingleton<ICompiledMapDependencyProvider, CompiledMapDependencyProvider>();
            services.AddSingleton<IMappingGenerationBackend, DynabeeMappingGenerationBackend>();
            services.AddSingleton<ICompiledMapRegistry, CompiledMapRegistry>();
            services.AddSingleton<IOctoProjectionBuilder, OctoProjectionBuilder>();
            services.AddScoped<IMapContextFactory, MapContextFactory>();
            services.AddScoped<IOctoMapper, OctoMapper>();
            services.AddSingleton(typeof(IOctoMapper<,>), typeof(OctoMapper<,>));

            return services;
        }

        private static OctoMapConfigurationBuilder BuildConfigurationBuilder(
            OctoMapOptions options,
            IReadOnlyCollection<OctoMapProfile> profiles,
            IReadOnlyCollection<Assembly> mapAssemblies,
            OctoMapRegistrationBuilder registrationBuilder)
        {
            var builder = new OctoMapConfigurationBuilder(options);
            var defaultOptions = options.Clone();
            foreach (var profile in profiles)
            {
                builder.ResetOptions(defaultOptions);
                builder.CurrentDeclarationSource = $"Profile {profile.GetType().FullName}";
                profile.Configure(builder);
            }

            builder.ResetOptions(defaultOptions);
            foreach (var assembly in mapAssemblies ?? Array.Empty<Assembly>())
            {
                RegisterInterfaceMaps(builder, assembly, registrationBuilder);
                RegisterAttributeMaps(builder, assembly, registrationBuilder);
            }

            ApplyAttributeMemberMaps(builder);
            return builder;
        }

        private static void RegisterInterfaceMaps(OctoMapConfigurationBuilder builder, Assembly assembly, OctoMapRegistrationBuilder registrationBuilder)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface || !registrationBuilder.AllowsMapType(type))
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
                        builder.CurrentDeclarationSource = $"IMapFrom on {type.FullName}";
                        builder.CreateMap(relatedType, type);
                    }

                    if (genericDefinition == typeof(IMapTo<>))
                    {
                        builder.CurrentDeclarationSource = $"IMapTo on {type.FullName}";
                        builder.CreateMap(type, relatedType);
                    }
                }
            }
        }

        private static void RegisterAttributeMaps(OctoMapConfigurationBuilder builder, Assembly assembly, OctoMapRegistrationBuilder registrationBuilder)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsAbstract || type.IsInterface || !registrationBuilder.AllowsMapType(type))
                {
                    continue;
                }

                foreach (var mapFrom in type.GetCustomAttributes<MapFromAttribute>())
                {
                    builder.CurrentDeclarationSource = $"MapFromAttribute on {type.FullName}";
                    builder.CreateMap(mapFrom.SourceType, type);
                }

                foreach (var mapTo in type.GetCustomAttributes<MapToAttribute>())
                {
                    builder.CurrentDeclarationSource = $"MapToAttribute on {type.FullName}";
                    builder.CreateMap(type, mapTo.DestinationType);
                }
            }
        }

        private static void ApplyAttributeMemberMaps(OctoMapConfigurationBuilder builder)
        {
            foreach (var map in builder.Maps.Values)
            {
                foreach (var destinationProperty in map.DestinationType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    if (map.MemberMaps.ContainsKey(destinationProperty.Name))
                    {
                        continue;
                    }

                    ApplyAttributeMemberMap(map, destinationProperty);
                }
            }
        }

        private static void ApplyAttributeMemberMap(TypeMap map, PropertyInfo destinationProperty)
        {
            if (destinationProperty.GetCustomAttribute<IgnoreMapAttribute>() != null)
            {
                map.GetOrAddMemberMap(destinationProperty).IsIgnored = true;
                return;
            }

            var mapName = destinationProperty.GetCustomAttribute<MapNameAttribute>();
            var nullSubstitute = destinationProperty.GetCustomAttribute<NullSubstituteAttribute>();
            if (mapName == null && nullSubstitute == null)
            {
                return;
            }

            var memberMap = map.GetOrAddMemberMap(destinationProperty);
            if (mapName != null)
            {
                var sourceProperty = map.SourceType.GetProperty(mapName.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (sourceProperty != null && sourceProperty.CanRead)
                {
                    memberMap.SourceExpression = CreateSourcePropertyExpression(map.SourceType, sourceProperty);
                }
            }

            if (nullSubstitute != null)
            {
                memberMap.HasNullSubstitute = true;
                memberMap.NullSubstitute = nullSubstitute.Value;
            }
        }

        private static LambdaExpression CreateSourcePropertyExpression(Type sourceType, PropertyInfo sourceProperty)
        {
            var parameter = Expression.Parameter(sourceType, "source");
            return Expression.Lambda(Expression.Property(parameter, sourceProperty), parameter);
        }
    }
}
