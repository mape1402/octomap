namespace OctoMap
{
    /// <summary>
    /// Defines a compiled single-source mapping operation.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    public interface IOctoMapping<TSource, TDestination>
    {
        /// <summary>
        /// Maps the specified source instance to the destination type.
        /// </summary>
        /// <param name="source">The source instance.</param>
        /// <param name="context">The mapping context, or null when the map does not require it.</param>
        /// <returns>The mapped destination instance.</returns>
        TDestination Map(TSource source, IMapContext context);
    }

    /// <summary>
    /// Defines a compiled single-source mapping operation that does not require a runtime context.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    public interface IOctoContextFreeMapping<TSource, TDestination>
    {
        /// <summary>
        /// Maps the specified source instance to the destination type.
        /// </summary>
        /// <param name="source">The source instance.</param>
        /// <returns>The mapped destination instance.</returns>
        TDestination MapContextFree(TSource source);
    }

    /// <summary>
    /// Defines a compiled existing-destination mapping operation.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    public interface IOctoExistingDestinationMapping<TSource, TDestination>
    {
        /// <summary>
        /// Maps the specified source instance onto an existing destination instance.
        /// </summary>
        /// <param name="source">The source instance.</param>
        /// <param name="destination">The existing destination instance.</param>
        /// <param name="context">The mapping context, or null when the map does not require it.</param>
        /// <returns>The mapped destination instance.</returns>
        TDestination MapToExisting(TSource source, TDestination destination, IMapContext context);
    }

    /// <summary>
    /// Defines a compiled existing-destination mapping operation that does not require a runtime context.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    public interface IOctoContextFreeExistingDestinationMapping<TSource, TDestination>
    {
        /// <summary>
        /// Maps the specified source instance onto an existing destination instance.
        /// </summary>
        /// <param name="source">The source instance.</param>
        /// <param name="destination">The existing destination instance.</param>
        /// <returns>The mapped destination instance.</returns>
        TDestination MapToExistingContextFree(TSource source, TDestination destination);
    }
}
