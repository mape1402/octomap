namespace OctoMap.Generation
{
    /// <summary>
    /// Represents a compiled mapper produced by a generation backend.
    /// </summary>
    public sealed class CompiledMap
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CompiledMap"/> class.
        /// </summary>
        /// <param name="mapper">The generated mapper instance.</param>
        /// <param name="mapperType">The generated mapper type.</param>
        public CompiledMap(object mapper, Type mapperType)
            : this(mapper, mapperType, null, null, null, null, null, null, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompiledMap"/> class.
        /// </summary>
        /// <param name="mapper">The generated mapper instance.</param>
        /// <param name="mapperType">The generated mapper type.</param>
        /// <param name="invoker">The compiled map invoker.</param>
        public CompiledMap(object mapper, Type mapperType, ICompiledMapInvoker invoker)
            : this(mapper, mapperType, invoker, null, null, null, null, null, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompiledMap"/> class.
        /// </summary>
        /// <param name="mapper">The generated mapper instance.</param>
        /// <param name="mapperType">The generated mapper type.</param>
        /// <param name="invoker">The compiled map invoker.</param>
        /// <param name="existingDestinationInvoker">The compiled existing destination map invoker.</param>
        public CompiledMap(
            object mapper,
            Type mapperType,
            ICompiledMapInvoker invoker,
            ICompiledMapInvoker existingDestinationInvoker)
            : this(mapper, mapperType, invoker, existingDestinationInvoker, null, null, null, null, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompiledMap"/> class.
        /// </summary>
        /// <param name="mapper">The generated mapper instance.</param>
        /// <param name="mapperType">The generated mapper type.</param>
        /// <param name="invoker">The compiled map invoker.</param>
        /// <param name="existingDestinationInvoker">The compiled existing destination map invoker.</param>
        /// <param name="typedInvoker">The typed single-source map invoker.</param>
        /// <param name="typedExistingDestinationInvoker">The typed existing destination map invoker.</param>
        /// <param name="contextFreeInvoker">The context-free typed single-source map invoker.</param>
        /// <param name="contextFreeExistingDestinationInvoker">The context-free typed existing destination map invoker.</param>
        /// <param name="requiresContext">A value indicating whether the map requires a runtime context.</param>
        public CompiledMap(
            object mapper,
            Type mapperType,
            ICompiledMapInvoker invoker,
            ICompiledMapInvoker existingDestinationInvoker,
            object typedInvoker,
            object typedExistingDestinationInvoker,
            object contextFreeInvoker,
            object contextFreeExistingDestinationInvoker,
            bool requiresContext)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            MapperType = mapperType ?? throw new ArgumentNullException(nameof(mapperType));
            Invoker = invoker;
            ExistingDestinationInvoker = existingDestinationInvoker;
            TypedInvoker = typedInvoker;
            TypedExistingDestinationInvoker = typedExistingDestinationInvoker;
            ContextFreeInvoker = contextFreeInvoker;
            ContextFreeExistingDestinationInvoker = contextFreeExistingDestinationInvoker;
            RequiresContext = requiresContext;
        }

        /// <summary>
        /// Gets the generated mapper instance.
        /// </summary>
        public object Mapper { get; }

        /// <summary>
        /// Gets the generated mapper type.
        /// </summary>
        public Type MapperType { get; }

        /// <summary>
        /// Gets the compiled map invoker.
        /// </summary>
        public ICompiledMapInvoker Invoker { get; }

        /// <summary>
        /// Gets the compiled existing destination map invoker.
        /// </summary>
        public ICompiledMapInvoker ExistingDestinationInvoker { get; }

        /// <summary>
        /// Gets the typed single-source map invoker.
        /// </summary>
        public object TypedInvoker { get; }

        /// <summary>
        /// Gets the typed existing destination map invoker.
        /// </summary>
        public object TypedExistingDestinationInvoker { get; }

        /// <summary>
        /// Gets the context-free typed single-source map invoker.
        /// </summary>
        public object ContextFreeInvoker { get; }

        /// <summary>
        /// Gets the context-free typed existing destination map invoker.
        /// </summary>
        public object ContextFreeExistingDestinationInvoker { get; }

        /// <summary>
        /// Gets whether the map requires a runtime context.
        /// </summary>
        public bool RequiresContext { get; }
    }
}
