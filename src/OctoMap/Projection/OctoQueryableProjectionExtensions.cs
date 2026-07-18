namespace OctoMap
{
    /// <summary>
    /// Provides queryable projection extensions.
    /// </summary>
    public static class OctoQueryableProjectionExtensions
    {
        /// <summary>
        /// Projects a queryable source sequence to the configured destination type.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="source">The source query.</param>
        /// <param name="projectionBuilder">The projection builder.</param>
        /// <returns>The projected query.</returns>
        public static IQueryable<TDestination> ProjectTo<TSource, TDestination>(
            this IQueryable<TSource> source,
            IOctoProjectionBuilder projectionBuilder)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (projectionBuilder == null)
            {
                throw new ArgumentNullException(nameof(projectionBuilder));
            }

            return source.Select(projectionBuilder.Build<TSource, TDestination>());
        }

        /// <summary>
        /// Projects a queryable source sequence to the configured destination type using the mapper facade.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="source">The source query.</param>
        /// <param name="mapper">The mapper facade.</param>
        /// <returns>The projected query.</returns>
        public static IQueryable<TDestination> ProjectTo<TSource, TDestination>(
            this IQueryable<TSource> source,
            IOctoMapper mapper)
        {
            if (mapper == null)
            {
                throw new ArgumentNullException(nameof(mapper));
            }

            return mapper.ProjectTo<TSource, TDestination>(source);
        }
    }
}
