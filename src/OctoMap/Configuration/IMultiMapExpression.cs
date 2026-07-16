namespace OctoMap
{
    using System.Linq.Expressions;

    /// <summary>
    /// Represents the fluent configuration surface for a multi-source type map.
    /// </summary>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    public interface IMultiMapExpression<TDestination>
    {
        /// <summary>
        /// Configures mappings contributed by one source type.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <param name="configure">The source map configuration callback.</param>
        /// <returns>The current multi-source map expression.</returns>
        IMultiMapExpression<TDestination> From<TSource>(Action<IMapExpression<TSource, TDestination>> configure);

        /// <summary>
        /// Configures a destination member from the multi-source context.
        /// </summary>
        /// <typeparam name="TMember">The destination member type.</typeparam>
        /// <param name="destinationMember">The destination member expression.</param>
        /// <param name="configure">The member configuration callback.</param>
        /// <returns>The current multi-source map expression.</returns>
        IMultiMapExpression<TDestination> ForMember<TMember>(
            Expression<Func<TDestination, TMember>> destinationMember,
            Action<IMultiSourceMemberMapExpression<TDestination, TMember>> configure);
    }
}
