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
        private readonly IOctoMapping<TSource, TDestination> _mapper;
        private readonly IOctoExistingDestinationMapping<TSource, TDestination> _existingDestinationMapper;
        private readonly IOctoContextFreeMapping<TSource, TDestination> _contextFreeMapper;
        private readonly IOctoContextFreeExistingDestinationMapping<TSource, TDestination> _contextFreeExistingDestinationMapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="GeneratedOctoMapperInvoker{TSource, TDestination}"/> class.
        /// </summary>
        /// <param name="mapper">The compiled mapper instance.</param>
        public GeneratedOctoMapperInvoker(IOctoMapping<TSource, TDestination> mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _existingDestinationMapper = mapper as IOctoExistingDestinationMapping<TSource, TDestination>;
            _contextFreeMapper = mapper as IOctoContextFreeMapping<TSource, TDestination>;
            _contextFreeExistingDestinationMapper = mapper as IOctoContextFreeExistingDestinationMapping<TSource, TDestination>;
        }

        /// <inheritdoc/>
        public TDestination Invoke(TSource source, IMapContext context)
            => _mapper.Map(source, context);

        /// <inheritdoc/>
        public TDestination Invoke(TSource source, TDestination destination, IMapContext context)
            => _existingDestinationMapper.MapToExisting(source, destination, context);

        /// <inheritdoc/>
        public TDestination Invoke(TSource source)
            => _contextFreeMapper.MapContextFree(source);

        /// <inheritdoc/>
        public TDestination Invoke(TSource source, TDestination destination)
            => _contextFreeExistingDestinationMapper.MapToExistingContextFree(source, destination);
    }
}
