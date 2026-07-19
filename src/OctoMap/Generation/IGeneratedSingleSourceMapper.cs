namespace OctoMap.Generation
{
    /// <summary>
    /// Defines the direct invocation contract implemented by generated single-source mappers.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    public interface IGeneratedSingleSourceMapper<TSource, TDestination>
    {
        /// <summary>
        /// Maps the specified source instance to the destination type.
        /// </summary>
        /// <param name="source">The source instance.</param>
        /// <param name="context">The mapping context, or null when the plan does not require it.</param>
        /// <returns>The mapped destination instance.</returns>
        TDestination Map(TSource source, IMapContext context);

        /// <summary>
        /// Maps the specified source instance onto an existing destination instance.
        /// </summary>
        /// <param name="source">The source instance.</param>
        /// <param name="destination">The existing destination instance.</param>
        /// <param name="context">The mapping context, or null when the plan does not require it.</param>
        /// <returns>The mapped destination instance.</returns>
        TDestination MapToExisting(TSource source, TDestination destination, IMapContext context);
    }

    /// <summary>
    /// Defines the direct invocation contract implemented by generated mappers that do not require a runtime context.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    public interface IGeneratedContextFreeMapper<TSource, TDestination>
    {
        /// <summary>
        /// Maps the specified source instance to the destination type.
        /// </summary>
        /// <param name="source">The source instance.</param>
        /// <returns>The mapped destination instance.</returns>
        TDestination MapContextFree(TSource source);

        /// <summary>
        /// Maps the specified source instance onto an existing destination instance.
        /// </summary>
        /// <param name="source">The source instance.</param>
        /// <param name="destination">The existing destination instance.</param>
        /// <returns>The mapped destination instance.</returns>
        TDestination MapToExistingContextFree(TSource source, TDestination destination);
    }
}
