namespace OctoMap
{
    /// <summary>
    /// Provides runtime object mapping operations.
    /// </summary>
    public interface IOctoMapper
    {
        /// <summary>
        /// Maps the specified source instance to a destination type.
        /// </summary>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="source">The source instance.</param>
        /// <returns>The mapped destination instance.</returns>
        TDestination Map<TDestination>(object source);

        /// <summary>
        /// Maps the specified source instance to a destination type.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="source">The source instance.</param>
        /// <returns>The mapped destination instance.</returns>
        TDestination Map<TSource, TDestination>(TSource source);

        /// <summary>
        /// Maps the specified source set to a destination type using an explicitly configured multi-source map.
        /// </summary>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="sources">The source set.</param>
        /// <returns>The mapped destination instance.</returns>
        TDestination Map<TDestination>(SourceSet sources);
    }
}
