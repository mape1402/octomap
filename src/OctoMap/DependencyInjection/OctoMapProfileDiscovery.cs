using System.Reflection;

namespace OctoMap.DependencyInjection
{
    /// <summary>
    /// Discovers profile instances from assemblies.
    /// </summary>
    internal sealed class OctoMapProfileDiscovery : IOctoMapProfileDiscovery
    {
        /// <inheritdoc/>
        public IReadOnlyCollection<OctoMapProfile> Discover(params Assembly[] assemblies)
        {
            if (assemblies == null || assemblies.Length == 0)
            {
                return Array.Empty<OctoMapProfile>();
            }

            var profiles = new List<OctoMapProfile>();
            foreach (var assembly in assemblies.Distinct())
            {
                profiles.AddRange(DiscoverFromAssembly(assembly));
            }

            return profiles;
        }

        private static IEnumerable<OctoMapProfile> DiscoverFromAssembly(Assembly assembly)
        {
            foreach (var type in assembly.GetTypes())
            {
                if (!typeof(OctoMapProfile).IsAssignableFrom(type) || type.IsAbstract || type.GetConstructor(Type.EmptyTypes) == null)
                {
                    continue;
                }

                if (Activator.CreateInstance(type) is OctoMapProfile profile)
                {
                    yield return profile;
                }
            }
        }
    }
}
