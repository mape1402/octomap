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

    /// <summary>
    /// Invokes a compiled single-source mapping method without allocating an argument array.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    public interface ICompiledMapInvoker<TSource, TDestination>
    {
        /// <summary>
        /// Invokes the compiled mapping method.
        /// </summary>
        /// <param name="source">The source instance.</param>
        /// <param name="context">The mapping context, or null when the plan does not require it.</param>
        /// <returns>The mapped destination object.</returns>
        TDestination Invoke(TSource source, IMapContext context);
    }

    /// <summary>
    /// Invokes a compiled existing-destination mapping method without allocating an argument array.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    public interface ICompiledExistingDestinationMapInvoker<TSource, TDestination>
    {
        /// <summary>
        /// Invokes the compiled mapping method.
        /// </summary>
        /// <param name="source">The source instance.</param>
        /// <param name="destination">The destination instance to update.</param>
        /// <param name="context">The mapping context, or null when the plan does not require it.</param>
        /// <returns>The mapped destination object.</returns>
        TDestination Invoke(TSource source, TDestination destination, IMapContext context);
    }

    /// <summary>
    /// Invokes a compiled context-free mapping method without allocating an argument array.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    public interface ICompiledContextFreeMapInvoker<TSource, TDestination>
    {
        /// <summary>
        /// Invokes the compiled mapping method.
        /// </summary>
        /// <param name="source">The source instance.</param>
        /// <returns>The mapped destination object.</returns>
        TDestination Invoke(TSource source);
    }

    /// <summary>
    /// Invokes a compiled context-free existing-destination mapping method without allocating an argument array.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    public interface ICompiledContextFreeExistingDestinationMapInvoker<TSource, TDestination>
    {
        /// <summary>
        /// Invokes the compiled mapping method.
        /// </summary>
        /// <param name="source">The source instance.</param>
        /// <param name="destination">The destination instance to update.</param>
        /// <returns>The mapped destination object.</returns>
        TDestination Invoke(TSource source, TDestination destination);
    }
}
