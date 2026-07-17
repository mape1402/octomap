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
    }
}
