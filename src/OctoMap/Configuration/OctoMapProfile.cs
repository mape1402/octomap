namespace OctoMap
{
    /// <summary>
    /// Provides a profile-based mapping configuration unit.
    /// </summary>
    public abstract class OctoMapProfile
    {
        /// <summary>
        /// Configures maps owned by this profile.
        /// </summary>
        /// <param name="builder">The configuration builder.</param>
        public abstract void Configure(IOctoMapConfigurationBuilder builder);
    }
}
