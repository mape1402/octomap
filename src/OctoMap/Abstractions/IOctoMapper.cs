namespace OctoMap
{
    /// <summary>
    /// Provides runtime object mapping operations.
    /// </summary>
    public interface IOctoMapper
    {
        /// <summary>
        /// Gets the projection builder used by queryable projection extensions.
        /// </summary>
        IOctoProjectionBuilder ProjectionBuilder { get; }

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
        /// Maps the specified source instance onto an existing destination instance.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="source">The source instance.</param>
        /// <param name="destination">The existing destination instance.</param>
        /// <returns>The mapped destination instance.</returns>
        TDestination Map<TSource, TDestination>(TSource source, TDestination destination);

        /// <summary>
        /// Maps the specified source set to a destination type using an explicitly configured multi-source map.
        /// </summary>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="sources">The source set.</param>
        /// <returns>The mapped destination instance.</returns>
        TDestination Map<TDestination>(SourceSet sources);

    }
}
