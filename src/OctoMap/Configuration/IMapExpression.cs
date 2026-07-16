namespace OctoMap
{
    using System.Linq.Expressions;

    /// <summary>
    /// Represents the fluent configuration surface for a type map.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    public interface IMapExpression<TSource, TDestination>
    {
        /// <summary>
        /// Configures a destination member.
        /// </summary>
        /// <typeparam name="TMember">The destination member type.</typeparam>
        /// <param name="destinationMember">The destination member expression.</param>
        /// <param name="configure">The member configuration callback.</param>
        /// <returns>The current map expression.</returns>
        IMapExpression<TSource, TDestination> ForMember<TMember>(
            Expression<Func<TDestination, TMember>> destinationMember,
            Action<IMemberMapExpression<TSource, TDestination, TMember>> configure);
    }
}
