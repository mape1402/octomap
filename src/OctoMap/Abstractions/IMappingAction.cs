namespace OctoMap
{
    /// <summary>
    /// Represents a lifecycle action executed before or after a runtime map.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    public interface IMappingAction<in TSource, in TDestination>
    {
        /// <summary>
        /// Executes the lifecycle action.
        /// </summary>
        /// <param name="source">The source object.</param>
        /// <param name="destination">The destination object.</param>
        /// <param name="context">The mapping context.</param>
        void Process(TSource source, TDestination destination, IMapContext context);
    }
}
