namespace OctoMap
{
    using System.Linq.Expressions;

    /// <summary>
    /// Represents the fluent configuration surface for a destination member.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <typeparam name="TMember">The destination member type.</typeparam>
    public interface IMemberMapExpression<TSource, TDestination, TMember>
    {
        /// <summary>
        /// Ignores this destination member.
        /// </summary>
        void Ignore();

        /// <summary>
        /// Maps this destination member from a source expression.
        /// </summary>
        /// <param name="sourceExpression">The source expression.</param>
        void MapFrom(Expression<Func<TSource, TMember>> sourceExpression);

        /// <summary>
        /// Configures a predicate that must pass before resolving the member value.
        /// </summary>
        /// <param name="conditionExpression">The precondition expression.</param>
        void PreCondition(Expression<Func<TSource, bool>> conditionExpression);

        /// <summary>
        /// Configures a predicate that must pass before assigning the resolved member value.
        /// </summary>
        /// <param name="conditionExpression">The condition expression.</param>
        void Condition(Expression<Func<TSource, bool>> conditionExpression);

        /// <summary>
        /// Configures a predicate that must pass before assigning the resolved member value.
        /// </summary>
        /// <param name="conditionExpression">The condition expression.</param>
        void Condition(Expression<Func<TSource, TMember, bool>> conditionExpression);

        /// <summary>
        /// Converts a source member value through a service resolved from the mapping context service provider.
        /// </summary>
        /// <typeparam name="TConverter">The converter type.</typeparam>
        /// <param name="sourceExpression">The source member expression.</param>
        void ConvertUsing<TConverter>(Expression<Func<TSource, object>> sourceExpression);

        /// <summary>
        /// Converts a source member value through a service resolved from the mapping context service provider.
        /// </summary>
        /// <typeparam name="TConverter">The converter type.</typeparam>
        /// <typeparam name="TSourceMember">The source member type.</typeparam>
        /// <param name="sourceExpression">The source member expression.</param>
        void ConvertUsing<TConverter, TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceExpression)
            where TConverter : IValueConverter<TSourceMember, TMember>;

        /// <summary>
        /// Resolves this destination member through a service resolved from the mapping context service provider.
        /// </summary>
        /// <typeparam name="TResolver">The resolver type.</typeparam>
        void ResolveUsing<TResolver>()
            where TResolver : IValueResolver<TSource, TDestination, TMember>;

        /// <summary>
        /// Maps this destination member from a constant value.
        /// </summary>
        /// <param name="value">The constant value.</param>
        void UseValue(TMember value);

        /// <summary>
        /// Uses the specified value when the resolved source value is null.
        /// </summary>
        /// <param name="value">The replacement value.</param>
        void NullSubstitute(TMember value);

        /// <summary>
        /// Configures whether a null source collection can map to a null destination collection for this member.
        /// </summary>
        /// <param name="allowNull">True to preserve null collections; false to map null collections to empty collections.</param>
        void AllowNullCollection(bool allowNull);

        /// <summary>
        /// Maps a null source collection to an empty destination collection for this member.
        /// </summary>
        void UseEmptyCollectionWhenNull();
    }
}
