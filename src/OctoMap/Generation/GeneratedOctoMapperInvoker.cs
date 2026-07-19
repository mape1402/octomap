namespace OctoMap.Generation
{
    /// <summary>
    /// Adapts a generated OctoMap mapper instance to the backend-neutral typed invoker contracts.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    internal sealed class GeneratedOctoMapperInvoker<TSource, TDestination> :
        ICompiledMapInvoker<TSource, TDestination>,
        ICompiledExistingDestinationMapInvoker<TSource, TDestination>,
        ICompiledContextFreeMapInvoker<TSource, TDestination>,
        ICompiledContextFreeExistingDestinationMapInvoker<TSource, TDestination>
    {
        private readonly IGeneratedSingleSourceMapper<TSource, TDestination> _mapper;
        private readonly IGeneratedContextFreeMapper<TSource, TDestination> _contextFreeMapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratedOctoMapperInvoker{TSource, TDestination}"/> class.
        /// </summary>
        /// <param name="mapper">The generated mapper instance.</param>
        public GeneratedOctoMapperInvoker(IGeneratedSingleSourceMapper<TSource, TDestination> mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _contextFreeMapper = mapper as IGeneratedContextFreeMapper<TSource, TDestination>;
        }

        /// <inheritdoc/>
        public TDestination Invoke(TSource source, IMapContext context)
            => _mapper.Map(source, context);

        /// <inheritdoc/>
        public TDestination Invoke(TSource source, TDestination destination, IMapContext context)
            => _mapper.MapToExisting(source, destination, context);

        /// <inheritdoc/>
        public TDestination Invoke(TSource source)
            => _contextFreeMapper.MapContextFree(source);

        /// <inheritdoc/>
        public TDestination Invoke(TSource source, TDestination destination)
            => _contextFreeMapper.MapToExistingContextFree(source, destination);
    }
}
