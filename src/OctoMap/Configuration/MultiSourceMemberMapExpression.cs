namespace OctoMap.Configuration
{
    using System.Linq.Expressions;

    /// <summary>
    /// Represents an internal multi-source member map expression.
    /// </summary>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <typeparam name="TMember">The destination member type.</typeparam>
    internal sealed class MultiSourceMemberMapExpression<TDestination, TMember> : IMultiSourceMemberMapExpression<TDestination, TMember>
    {
        private readonly MultiSourceMemberMap _memberMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiSourceMemberMapExpression{TDestination, TMember}"/> class.
        /// </summary>
        /// <param name="memberMap">The member map.</param>
        public MultiSourceMemberMapExpression(MultiSourceMemberMap memberMap)
        {
            _memberMap = memberMap ?? throw new ArgumentNullException(nameof(memberMap));
        }

        /// <inheritdoc/>
        public void MapFrom(Expression<Func<IMultiSourceMapContext, TMember>> sourceExpression)
        {
            _memberMap.SourceExpression = sourceExpression ?? throw new ArgumentNullException(nameof(sourceExpression));
        }
    }
}
