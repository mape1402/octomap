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
            _memberMap.HasConstantValue = false;
            _memberMap.ResolverType = null;
            _memberMap.ConverterType = null;
            _memberMap.ConverterSourceExpression = null;
        }

        /// <inheritdoc/>
        public void MapFrom(Expression<Func<TSource, TMember>> sourceExpression)
        {
            _memberMap.IsIgnored = false;
            _memberMap.SourceExpression = sourceExpression ?? throw new ArgumentNullException(nameof(sourceExpression));
            _memberMap.HasConstantValue = false;
            _memberMap.ResolverType = null;
            _memberMap.ConverterType = null;
            _memberMap.ConverterSourceExpression = null;
        }

        /// <inheritdoc/>
        public void PreCondition(Expression<Func<TSource, bool>> conditionExpression)
        {
            _memberMap.PreConditionExpression = conditionExpression ?? throw new ArgumentNullException(nameof(conditionExpression));
        }

        /// <inheritdoc/>
        public void Condition(Expression<Func<TSource, bool>> conditionExpression)
        {
            _memberMap.ConditionExpression = conditionExpression ?? throw new ArgumentNullException(nameof(conditionExpression));
        }

        /// <inheritdoc/>
        public void Condition(Expression<Func<TSource, TMember, bool>> conditionExpression)
        {
            _memberMap.ConditionExpression = conditionExpression ?? throw new ArgumentNullException(nameof(conditionExpression));
        }

        /// <inheritdoc/>
        public void ConvertUsing<TConverter>(Expression<Func<TSource, object>> sourceExpression)
        {
            if (sourceExpression == null)
            {
                throw new ArgumentNullException(nameof(sourceExpression));
            }

            SetConverter<TConverter>(NormalizeObjectExpression(sourceExpression));
        }

        /// <inheritdoc/>
        public void ConvertUsing<TConverter, TSourceMember>(Expression<Func<TSource, TSourceMember>> sourceExpression)
            where TConverter : IValueConverter<TSourceMember, TMember>
        {
            if (sourceExpression == null)
            {
                throw new ArgumentNullException(nameof(sourceExpression));
            }

            SetConverter<TConverter>(sourceExpression);
        }

        private void SetConverter<TConverter>(LambdaExpression sourceExpression)
        {
            _memberMap.IsIgnored = false;
            _memberMap.SourceExpression = null;
            _memberMap.HasConstantValue = false;
            _memberMap.ResolverType = null;
            _memberMap.ConverterType = typeof(TConverter);
            _memberMap.ConverterSourceExpression = sourceExpression;
        }

        private static LambdaExpression NormalizeObjectExpression(Expression<Func<TSource, object>> sourceExpression)
        {
            var body = sourceExpression.Body;
            if (body is UnaryExpression unary
                && (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked)
                && unary.Type == typeof(object))
            {
                body = unary.Operand;
            }

            return Expression.Lambda(body, sourceExpression.Parameters);
        }

        /// <inheritdoc/>
        public void ResolveUsing<TResolver>()
            where TResolver : IValueResolver<TSource, TDestination, TMember>
        {
            _memberMap.IsIgnored = false;
            _memberMap.SourceExpression = null;
            _memberMap.HasConstantValue = false;
            _memberMap.ResolverType = typeof(TResolver);
            _memberMap.ConverterType = null;
            _memberMap.ConverterSourceExpression = null;
        }

        /// <inheritdoc/>
        public void UseValue(TMember value)
        {
            _memberMap.IsIgnored = false;
            _memberMap.SourceExpression = null;
            _memberMap.HasConstantValue = true;
            _memberMap.ConstantValue = value;
            _memberMap.ResolverType = null;
            _memberMap.ConverterType = null;
            _memberMap.ConverterSourceExpression = null;
        }

        /// <inheritdoc/>
        public void NullSubstitute(TMember value)
        {
            _memberMap.HasNullSubstitute = true;
            _memberMap.NullSubstitute = value;
        }

        /// <inheritdoc/>
        public void AllowNullCollection(bool allowNull)
        {
            _memberMap.AllowNullCollection = allowNull;
        }

        /// <inheritdoc/>
        public void UseEmptyCollectionWhenNull()
        {
            _memberMap.AllowNullCollection = false;
        }
    }
}
