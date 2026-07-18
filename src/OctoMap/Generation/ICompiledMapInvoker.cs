namespace OctoMap.Generation
{
    /// <summary>
    /// Invokes a compiled mapping method without exposing generation-backend-specific contracts.
    /// </summary>
    public interface ICompiledMapInvoker
    {
        /// <summary>
        /// Invokes the compiled mapping method.
        /// </summary>
        /// <param name="arguments">The method arguments.</param>
        /// <returns>The mapped destination object.</returns>
        object Invoke(IReadOnlyList<object> arguments);
    }
}
