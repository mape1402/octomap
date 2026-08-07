using System.Linq.Expressions;

namespace OctoMap.Testing
{
    /// <summary>
    /// Provides the default OctoMap testing mapper implementation.
    /// </summary>
    internal sealed class OctoMapTestingMapper : IOctoMapTestingAdapter
    {
        private readonly IOctoMapper _mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="OctoMapTestingMapper"/> class.
        /// </summary>
        /// <param name="mapper">The OctoMap mapper.</param>
        /// <param name="configuration">The OctoMap configuration.</param>
        public OctoMapTestingMapper(IOctoMapper mapper, IOctoMapConfiguration configuration)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        }

        /// <inheritdoc/>
        public IOctoMapConfiguration Configuration { get; }

        /// <inheritdoc/>
        public IOctoProjectionBuilder ProjectionBuilder => _mapper.ProjectionBuilder;

        /// <inheritdoc/>
        public Task AssertConfigurationIsValidAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Configuration.AssertValid();
            _mapper.CompileMappings();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<TDestination> MapAsync<TDestination>(object source, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_mapper.Map<TDestination>(source));
        }

        /// <inheritdoc/>
        public Task<TDestination> MapAsync<TSource, TDestination>(TSource source, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_mapper.Map<TSource, TDestination>(source));
        }

        /// <inheritdoc/>
        public Task<TDestination> MapAsync<TSource, TDestination>(TSource source, TDestination destination, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_mapper.Map(source, destination));
        }

        /// <inheritdoc/>
        public Task<TDestination> MapAsync<TDestination>(SourceSet sources, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_mapper.Map<TDestination>(sources));
        }

        /// <inheritdoc/>
        public IQueryable<TDestination> ProjectTo<TDestination>(IQueryable source, object parameters = null)
            => source.ProjectTo<TDestination>(ProjectionBuilder, parameters);

        /// <inheritdoc/>
        public Expression<Func<TSource, TDestination>> BuildProjection<TSource, TDestination>(object parameters = null)
            => ProjectionBuilder.Build<TSource, TDestination>(parameters);
    }
}
