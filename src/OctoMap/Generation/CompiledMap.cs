namespace OctoMap.Generation
{
    using DynaBee.FluentApi.Invocation;

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
            : this(mapper, mapperType, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompiledMap"/> class.
        /// </summary>
        /// <param name="mapper">The generated mapper instance.</param>
        /// <param name="mapperType">The generated mapper type.</param>
        /// <param name="invoker">The generated method invoker.</param>
        public CompiledMap(object mapper, Type mapperType, IDynaBeeBoundMethodInvoker invoker)
        {
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            MapperType = mapperType ?? throw new ArgumentNullException(nameof(mapperType));
            Invoker = invoker;
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
        /// Gets the generated method invoker.
        /// </summary>
        public IDynaBeeBoundMethodInvoker Invoker { get; }
    }
}
