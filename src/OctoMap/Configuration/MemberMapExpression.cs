namespace OctoMap.Configuration
{
    using System.Linq.Expressions;

    /// <summary>
    /// Represents an internal member map expression.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <typeparam name="TMember">The destination member type.</typeparam>
    internal sealed class MemberMapExpression<TSource, TDestination, TMember> : IMemberMapExpression<TSource, TDestination, TMember>
    {
        private readonly MemberMap _memberMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemberMapExpression{TSource, TDestination, TMember}"/> class.
        /// </summary>
        /// <param name="memberMap">The member map.</param>
        public MemberMapExpression(MemberMap memberMap)
        {
            _memberMap = memberMap ?? throw new ArgumentNullException(nameof(memberMap));
        }

        /// <inheritdoc/>
        public void Ignore()
        {
            _memberMap.IsIgnored = true;
            _memberMap.SourceExpression = null;
        }

        /// <inheritdoc/>
        public void MapFrom(Expression<Func<TSource, TMember>> sourceExpression)
        {
            _memberMap.IsIgnored = false;
            _memberMap.SourceExpression = sourceExpression ?? throw new ArgumentNullException(nameof(sourceExpression));
        }
    }
}
