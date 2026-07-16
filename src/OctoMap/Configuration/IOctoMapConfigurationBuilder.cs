namespace OctoMap
{
    /// <summary>
    /// Builds OctoMap configuration from profiles.
    /// </summary>
    public interface IOctoMapConfigurationBuilder
    {
        /// <summary>
        /// Creates a map between the source and destination types.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <returns>The map expression for further configuration.</returns>
        IMapExpression<TSource, TDestination> CreateMap<TSource, TDestination>();

        /// <summary>
        /// Creates a map between the source and destination types.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destinationType">The destination type.</param>
        void CreateMap(Type sourceType, Type destinationType);
    }
}
