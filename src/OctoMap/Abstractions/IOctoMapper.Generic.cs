namespace OctoMap
{
    /// <summary>
    /// Provides typed mapping operations for a specific source and destination pair.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    public interface IOctoMapper<TSource, TDestination>
    {
        /// <summary>
        /// Maps the specified source instance to the destination type.
        /// </summary>
        /// <param name="source">The source instance.</param>
        /// <param name="context">The mapping context.</param>
        /// <returns>The mapped destination instance.</returns>
        TDestination Map(TSource source, IMapContext context);

        /// <summary>
        /// Maps the specified source instance onto an existing destination instance.
        /// </summary>
        /// <param name="source">The source instance.</param>
        /// <param name="destination">The existing destination instance.</param>
        /// <param name="context">The mapping context.</param>
        /// <returns>The mapped destination instance.</returns>
        TDestination Map(TSource source, TDestination destination, IMapContext context);
    }
}
