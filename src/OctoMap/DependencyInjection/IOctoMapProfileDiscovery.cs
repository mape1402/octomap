using System.Reflection;

namespace OctoMap.DependencyInjection
{
    /// <summary>
    /// Discovers OctoMap profiles from assemblies.
    /// </summary>
    public interface IOctoMapProfileDiscovery
    {
        /// <summary>
        /// Discovers profile instances from the specified assemblies.
        /// </summary>
        /// <param name="assemblies">The assemblies to scan.</param>
        /// <returns>The discovered profiles.</returns>
        IReadOnlyCollection<OctoMapProfile> Discover(params Assembly[] assemblies);
    }
}
