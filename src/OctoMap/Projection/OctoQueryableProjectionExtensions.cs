using System.Linq.Expressions;

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
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="source">The source query.</param>
        /// <param name="projectionBuilder">The projection builder.</param>
        /// <returns>The projected query.</returns>
        public static IQueryable<TDestination> ProjectTo<TDestination>(
            this IQueryable source,
            IOctoProjectionBuilder projectionBuilder)
            => ProjectTo<TDestination>(source, projectionBuilder, null);

        /// <summary>
        /// Projects a queryable source sequence to the configured destination type using external parameters.
        /// </summary>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="source">The source query.</param>
        /// <param name="projectionBuilder">The projection builder.</param>
        /// <param name="parameters">The projection parameters.</param>
        /// <returns>The projected query.</returns>
        public static IQueryable<TDestination> ProjectTo<TDestination>(
            this IQueryable source,
            IOctoProjectionBuilder projectionBuilder,
            object parameters)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (projectionBuilder == null)
            {
                throw new ArgumentNullException(nameof(projectionBuilder));
            }

            var projection = projectionBuilder.Build(source.ElementType, typeof(TDestination), parameters);
            var select = Expression.Call(
                typeof(Queryable),
                nameof(Queryable.Select),
                new[] { source.ElementType, typeof(TDestination) },
                source.Expression,
                Expression.Quote(projection));

            return source.Provider.CreateQuery<TDestination>(select);
        }
    }
}
