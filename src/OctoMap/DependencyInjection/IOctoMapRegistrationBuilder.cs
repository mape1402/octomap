using System.Reflection;

namespace OctoMap
{
    /// <summary>
    /// Configures OctoMap service registration and map discovery.
    /// </summary>
    public interface IOctoMapRegistrationBuilder
    {
        /// <summary>
        /// Gets the OctoMap options.
        /// </summary>
        OctoMapOptions Options { get; }

        /// <summary>
        /// Adds a profile instance.
        /// </summary>
        /// <param name="profile">The profile instance.</param>
        /// <returns>The same registration builder.</returns>
        IOctoMapRegistrationBuilder AddProfile(OctoMapProfile profile);

        /// <summary>
        /// Adds a profile by type.
        /// </summary>
        /// <typeparam name="TProfile">The profile type.</typeparam>
        /// <returns>The same registration builder.</returns>
        IOctoMapRegistrationBuilder AddProfile<TProfile>()
            where TProfile : OctoMapProfile, new();

        /// <summary>
        /// Adds profiles from an assembly.
        /// </summary>
        /// <param name="assembly">The assembly to scan.</param>
        /// <returns>The same registration builder.</returns>
        IOctoMapRegistrationBuilder AddProfilesFromAssembly(Assembly assembly);

        /// <summary>
        /// Adds profiles, interface maps, and attribute maps from an assembly.
        /// </summary>
        /// <param name="assembly">The assembly to scan.</param>
        /// <returns>The same registration builder.</returns>
        IOctoMapRegistrationBuilder AddMaps(Assembly assembly);

        /// <summary>
        /// Filters profile types discovered from assemblies.
        /// </summary>
        /// <param name="predicate">The profile type predicate.</param>
        /// <returns>The same registration builder.</returns>
        IOctoMapRegistrationBuilder WhereProfile(Func<Type, bool> predicate);

        /// <summary>
        /// Filters model types used for interface and attribute map discovery.
        /// </summary>
        /// <param name="predicate">The model type predicate.</param>
        /// <returns>The same registration builder.</returns>
        IOctoMapRegistrationBuilder WhereMapType(Func<Type, bool> predicate);
    }
}
