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
    }
}
