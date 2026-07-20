using DynaBee.FluentApi.Invocation;

namespace OctoMap.Generation.Dynabee
{
    /// <summary>
    /// Adapts DynaBee dynamic method dispatch to OctoMap's backend-neutral invoker contract.
    /// </summary>
    internal sealed class DynabeeCompiledMapInvoker : ICompiledMapInvoker
    {
        private readonly Func<IReadOnlyList<object>, object> _invoke;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynabeeCompiledMapInvoker"/> class.
        /// </summary>
        /// <param name="invoker">The DynaBee bound method invoker.</param>
        public DynabeeCompiledMapInvoker(IDynaBeeBoundMethodInvoker invoker)
        {
            if (invoker == null)
            {
                throw new ArgumentNullException(nameof(invoker));
            }

            _invoke = invoker.Invoke;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DynabeeCompiledMapInvoker"/> class.
        /// </summary>
        /// <param name="adapter">The DynaBee object method adapter.</param>
        public DynabeeCompiledMapInvoker(IDynaBeeObjectMethodAdapter adapter)
        {
            if (adapter == null)
            {
                throw new ArgumentNullException(nameof(adapter));
            }

            _invoke = adapter.Invoke;
        }

        /// <inheritdoc/>
        public object Invoke(IReadOnlyList<object> arguments)
            => _invoke(arguments);
    }
}
