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
        /// Creates and returns the reverse map expression.
        /// </summary>
        /// <returns>The reverse map expression.</returns>
        IMapExpression<TDestination, TSource> ReverseMap();

        /// <summary>
        /// Configures how the destination object is constructed.
        /// </summary>
        /// <param name="constructionExpression">The destination construction expression.</param>
        /// <returns>The current map expression.</returns>
        IMapExpression<TSource, TDestination> ConstructUsing(Expression<Func<TSource, TDestination>> constructionExpression);

        /// <summary>
        /// Configures an action that runs before member assignment.
        /// </summary>
        /// <param name="action">The lifecycle action.</param>
        /// <returns>The current map expression.</returns>
        IMapExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination, IMapContext> action);

        /// <summary>
        /// Configures a service action that runs before member assignment.
        /// </summary>
        /// <typeparam name="TAction">The lifecycle action type.</typeparam>
        /// <returns>The current map expression.</returns>
        IMapExpression<TSource, TDestination> BeforeMap<TAction>()
            where TAction : IMappingAction<TSource, TDestination>;

        /// <summary>
        /// Configures an action that runs after member assignment.
        /// </summary>
        /// <param name="action">The lifecycle action.</param>
        /// <returns>The current map expression.</returns>
        IMapExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination, IMapContext> action);

        /// <summary>
        /// Configures a service action that runs after member assignment.
        /// </summary>
        /// <typeparam name="TAction">The lifecycle action type.</typeparam>
        /// <returns>The current map expression.</returns>
        IMapExpression<TSource, TDestination> AfterMap<TAction>()
            where TAction : IMappingAction<TSource, TDestination>;

        /// <summary>
        /// Includes explicit member configuration from a base map.
        /// </summary>
        /// <typeparam name="TBaseSource">The base source type.</typeparam>
        /// <typeparam name="TBaseDestination">The base destination type.</typeparam>
        /// <returns>The current map expression.</returns>
        IMapExpression<TSource, TDestination> IncludeBase<TBaseSource, TBaseDestination>();

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

        /// <summary>
        /// Configures a nested destination member path.
        /// </summary>
        /// <typeparam name="TMember">The destination member type.</typeparam>
        /// <param name="destinationPath">The destination member path expression.</param>
        /// <param name="configure">The member configuration callback.</param>
        /// <returns>The current map expression.</returns>
        IMapExpression<TSource, TDestination> ForPath<TMember>(
            Expression<Func<TDestination, TMember>> destinationPath,
            Action<IMemberMapExpression<TSource, TDestination, TMember>> configure);
    }
}
