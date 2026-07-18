namespace OctoMap.Runtime
{
    /// <summary>
    /// Executes inline lifecycle actions captured from configuration.
    /// </summary>
    internal sealed class InlineLifecycleActionRegistry : IInlineLifecycleActionRegistry
    {
        private readonly IReadOnlyDictionary<int, Action<object, object, IMapContext>> _actions;

        /// <summary>
        /// Initializes a new instance of the <see cref="InlineLifecycleActionRegistry"/> class.
        /// </summary>
        /// <param name="actions">The configured actions.</param>
        public InlineLifecycleActionRegistry(IReadOnlyDictionary<int, Action<object, object, IMapContext>> actions)
        {
            _actions = actions ?? throw new ArgumentNullException(nameof(actions));
        }

        /// <inheritdoc/>
        public void Execute(int id, object source, object destination, IMapContext context)
        {
            if (!_actions.TryGetValue(id, out var action))
            {
                throw new InvalidOperationException($"Inline lifecycle action '{id}' is not registered.");
            }

            action(source, destination, context);
        }
    }
}
