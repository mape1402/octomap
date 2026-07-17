namespace OctoMap
{
    /// <summary>
    /// Resolves a destination member value using the source, destination, and map context.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <typeparam name="TMember">The destination member type.</typeparam>
    public interface IValueResolver<TSource, TDestination, TMember>
    {
        /// <summary>
        /// Resolves the destination member value.
        /// </summary>
        /// <param name="source">The source instance.</param>
        /// <param name="destination">The destination instance being mapped.</param>
        /// <param name="context">The mapping context.</param>
        /// <returns>The resolved member value.</returns>
        TMember Resolve(TSource source, TDestination destination, IMapContext context);
    }
}
