using DynaBee.FluentApi.Invocation;

namespace OctoMap.Generation.Dynabee
{
    /// <summary>
    /// Adapts a DynaBee bound method invoker to OctoMap's backend-neutral invoker contract.
    /// </summary>
    internal sealed class DynabeeCompiledMapInvoker : ICompiledMapInvoker
    {
        private readonly IDynaBeeBoundMethodInvoker _invoker;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynabeeCompiledMapInvoker"/> class.
        /// </summary>
        /// <param name="invoker">The DynaBee bound method invoker.</param>
        public DynabeeCompiledMapInvoker(IDynaBeeBoundMethodInvoker invoker)
        {
            _invoker = invoker ?? throw new ArgumentNullException(nameof(invoker));
        }

        /// <inheritdoc/>
        public object Invoke(IReadOnlyList<object> arguments)
            => _invoker.Invoke(arguments);
    }

}
