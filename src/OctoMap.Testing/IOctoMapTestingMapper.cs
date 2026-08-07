using System.Linq.Expressions;

namespace OctoMap.Testing
{
    /// <summary>
    /// Provides asynchronous test-facing access to OctoMap configuration, runtime mapping, and projection helpers.
    /// </summary>
    public interface IOctoMapTestingMapper
    {
        /// <summary>
        /// Gets the underlying OctoMap configuration used by the testing mapper.
        /// </summary>
        IOctoMapConfiguration Configuration { get; }

        /// <summary>
        /// Gets the projection builder used by OctoMap queryable projection extensions.
        /// </summary>
        IOctoProjectionBuilder ProjectionBuilder { get; }

        /// <summary>
        /// Validates all registered OctoMap configuration.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A completed task when configuration is valid.</returns>
        Task AssertConfigurationIsValidAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Maps the specified source instance to the destination type.
        /// </summary>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="source">The source instance.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The mapped destination instance.</returns>
        Task<TDestination> MapAsync<TDestination>(object source, CancellationToken cancellationToken = default);

        /// <summary>
        /// Maps the specified source instance to the destination type.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="source">The source instance.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The mapped destination instance.</returns>
        Task<TDestination> MapAsync<TSource, TDestination>(TSource source, CancellationToken cancellationToken = default);

        /// <summary>
        /// Maps the specified source instance onto an existing destination instance.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="source">The source instance.</param>
        /// <param name="destination">The existing destination instance.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The updated destination instance.</returns>
        Task<TDestination> MapAsync<TSource, TDestination>(TSource source, TDestination destination, CancellationToken cancellationToken = default);

        /// <summary>
        /// Maps the specified source set to the destination type using an explicitly configured multi-source map.
        /// </summary>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="sources">The source set.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The mapped destination instance.</returns>
        Task<TDestination> MapAsync<TDestination>(SourceSet sources, CancellationToken cancellationToken = default);

        /// <summary>
        /// Projects the specified queryable source sequence to the destination type.
        /// </summary>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="source">The source query.</param>
        /// <param name="parameters">The projection parameters.</param>
        /// <returns>The projected query.</returns>
        IQueryable<TDestination> ProjectTo<TDestination>(IQueryable source, object parameters = null);

        /// <summary>
        /// Builds a projection expression for the specified source and destination types.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="parameters">The projection parameters.</param>
        /// <returns>The projection expression.</returns>
        Expression<Func<TSource, TDestination>> BuildProjection<TSource, TDestination>(object parameters = null);
    }
}
