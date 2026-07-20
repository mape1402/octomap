namespace OctoMap.Generation.Dynabee
{
    /// <summary>
    /// Adapts a DynaBee-created typed map delegate to OctoMap's typed invoker contract.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    internal sealed class DynabeeTypedMapInvoker<TSource, TDestination> : ICompiledMapInvoker<TSource, TDestination>
    {
        private readonly Func<TSource, IMapContext, TDestination> _map;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynabeeTypedMapInvoker{TSource, TDestination}"/> class.
        /// </summary>
        /// <param name="map">The DynaBee-created typed map delegate.</param>
        public DynabeeTypedMapInvoker(Func<TSource, IMapContext, TDestination> map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
        }

        /// <inheritdoc/>
        public TDestination Invoke(TSource source, IMapContext context)
            => _map(source, context);
    }

    /// <summary>
    /// Adapts a DynaBee-created existing-destination map delegate to OctoMap's typed invoker contract.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    internal sealed class DynabeeTypedExistingDestinationMapInvoker<TSource, TDestination> : ICompiledExistingDestinationMapInvoker<TSource, TDestination>
    {
        private readonly Func<TSource, TDestination, IMapContext, TDestination> _map;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynabeeTypedExistingDestinationMapInvoker{TSource, TDestination}"/> class.
        /// </summary>
        /// <param name="map">The DynaBee-created typed existing-destination map delegate.</param>
        public DynabeeTypedExistingDestinationMapInvoker(Func<TSource, TDestination, IMapContext, TDestination> map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
        }

        /// <inheritdoc/>
        public TDestination Invoke(TSource source, TDestination destination, IMapContext context)
            => _map(source, destination, context);
    }

    /// <summary>
    /// Adapts a DynaBee-created context-free map delegate to OctoMap's typed invoker contract.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    internal sealed class DynabeeTypedContextFreeMapInvoker<TSource, TDestination> : ICompiledContextFreeMapInvoker<TSource, TDestination>
    {
        private readonly Func<TSource, TDestination> _map;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynabeeTypedContextFreeMapInvoker{TSource, TDestination}"/> class.
        /// </summary>
        /// <param name="map">The DynaBee-created typed context-free map delegate.</param>
        public DynabeeTypedContextFreeMapInvoker(Func<TSource, TDestination> map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
        }

        /// <inheritdoc/>
        public TDestination Invoke(TSource source)
            => _map(source);
    }

    /// <summary>
    /// Adapts a DynaBee-created context-free existing-destination map delegate to OctoMap's typed invoker contract.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    internal sealed class DynabeeTypedContextFreeExistingDestinationMapInvoker<TSource, TDestination> : ICompiledContextFreeExistingDestinationMapInvoker<TSource, TDestination>
    {
        private readonly Func<TSource, TDestination, TDestination> _map;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynabeeTypedContextFreeExistingDestinationMapInvoker{TSource, TDestination}"/> class.
        /// </summary>
        /// <param name="map">The DynaBee-created typed context-free existing-destination map delegate.</param>
        public DynabeeTypedContextFreeExistingDestinationMapInvoker(Func<TSource, TDestination, TDestination> map)
        {
            _map = map ?? throw new ArgumentNullException(nameof(map));
        }

        /// <inheritdoc/>
        public TDestination Invoke(TSource source, TDestination destination)
            => _map(source, destination);
    }
}
