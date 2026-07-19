using System.Reflection;

namespace OctoMap.DependencyInjection
{
    /// <summary>
    /// Collects OctoMap registration configuration.
    /// </summary>
    internal sealed class OctoMapRegistrationBuilder : IOctoMapRegistrationBuilder
    {
        private readonly List<OctoMapProfile> _profiles = new();
        private readonly List<Assembly> _profileAssemblies = new();
        private readonly List<Assembly> _mapAssemblies = new();
        private readonly List<Func<Type, bool>> _profileFilters = new();
        private readonly List<Func<Type, bool>> _mapTypeFilters = new();

        /// <summary>
        /// Gets the explicitly registered profile instances.
        /// </summary>
        public IReadOnlyList<OctoMapProfile> Profiles => _profiles;

        /// <summary>
        /// Gets the assemblies used for profile discovery.
        /// </summary>
        public IReadOnlyList<Assembly> ProfileAssemblies => _profileAssemblies;

        /// <summary>
        /// Gets the assemblies used for map declaration discovery.
        /// </summary>
        public IReadOnlyList<Assembly> MapAssemblies => _mapAssemblies;

        /// <inheritdoc/>
        public OctoMapOptions Options { get; } = new();

        /// <inheritdoc/>
        public IOctoMapRegistrationBuilder AddProfile(OctoMapProfile profile)
        {
            _profiles.Add(profile ?? throw new ArgumentNullException(nameof(profile)));
            return this;
        }

        /// <inheritdoc/>
        public IOctoMapRegistrationBuilder AddProfile<TProfile>()
            where TProfile : OctoMapProfile, new()
            => AddProfile(new TProfile());

        /// <inheritdoc/>
        public IOctoMapRegistrationBuilder AddProfilesFromAssembly(Assembly assembly)
        {
            _profileAssemblies.Add(assembly ?? throw new ArgumentNullException(nameof(assembly)));
            return this;
        }

        /// <inheritdoc/>
        public IOctoMapRegistrationBuilder AddMaps(Assembly assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            _profileAssemblies.Add(assembly);
            _mapAssemblies.Add(assembly);
            return this;
        }

        /// <inheritdoc/>
        public IOctoMapRegistrationBuilder WhereProfile(Func<Type, bool> predicate)
        {
            _profileFilters.Add(predicate ?? throw new ArgumentNullException(nameof(predicate)));
            return this;
        }

        /// <inheritdoc/>
        public IOctoMapRegistrationBuilder WhereMapType(Func<Type, bool> predicate)
        {
            _mapTypeFilters.Add(predicate ?? throw new ArgumentNullException(nameof(predicate)));
            return this;
        }

        /// <summary>
        /// Checks whether a profile type is allowed.
        /// </summary>
        /// <param name="type">The profile type.</param>
        /// <returns>True when the type is allowed.</returns>
        public bool AllowsProfile(Type type)
            => _profileFilters.All(x => x(type));

        /// <summary>
        /// Checks whether a map type is allowed.
        /// </summary>
        /// <param name="type">The map type.</param>
        /// <returns>True when the type is allowed.</returns>
        public bool AllowsMapType(Type type)
            => _mapTypeFilters.All(x => x(type));
    }
}
