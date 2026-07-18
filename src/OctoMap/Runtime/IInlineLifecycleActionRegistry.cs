namespace OctoMap.Runtime
{
    /// <summary>
    /// Executes configured inline lifecycle actions.
    /// </summary>
    public interface IInlineLifecycleActionRegistry
    {
        /// <summary>
        /// Executes the configured inline lifecycle action.
        /// </summary>
        /// <param name="id">The action identifier.</param>
        /// <param name="source">The source object.</param>
        /// <param name="destination">The destination object.</param>
        /// <param name="context">The map context.</param>
        void Execute(int id, object source, object destination, IMapContext context);
    }
}
