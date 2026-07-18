namespace OctoMap
{
    using System.Linq.Expressions;

    /// <summary>
    /// Represents the fluent configuration surface for a multi-source destination member.
    /// </summary>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <typeparam name="TMember">The destination member type.</typeparam>
    public interface IMultiSourceMemberMapExpression<TDestination, TMember>
    {
        /// <summary>
        /// Maps this destination member from the multi-source context.
        /// </summary>
        /// <param name="sourceExpression">The source context expression.</param>
        void MapFrom(Expression<Func<IMultiSourceMapContext, TMember>> sourceExpression);

        /// <summary>
        /// Configures whether null resolved source values should skip assignment for this member.
        /// </summary>
        /// <param name="ignoreNull">True to skip null values; false to assign null values.</param>
        void IgnoreNullSourceValue(bool ignoreNull = true);
    }
}
