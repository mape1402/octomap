namespace OctoMap.Configuration
{
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Represents an internal type map expression.
    /// </summary>
    /// <typeparam name="TSource">The source type.</typeparam>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    internal sealed class MapExpression<TSource, TDestination> : IMapExpression<TSource, TDestination>
    {
        private readonly TypeMap _typeMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="MapExpression{TSource, TDestination}"/> class.
        /// </summary>
        /// <param name="typeMap">The type map.</param>
        public MapExpression(TypeMap typeMap)
        {
            _typeMap = typeMap ?? throw new ArgumentNullException(nameof(typeMap));
        }

        /// <inheritdoc/>
        public IMapExpression<TSource, TDestination> ConstructUsing(Expression<Func<TSource, TDestination>> constructionExpression)
        {
            if (constructionExpression == null)
            {
                throw new ArgumentNullException(nameof(constructionExpression));
            }

            _typeMap.ConstructionExpression = constructionExpression;
            return this;
        }

        /// <inheritdoc/>
        public IMapExpression<TSource, TDestination> ForMember<TMember>(
            Expression<Func<TDestination, TMember>> destinationMember,
            Action<IMemberMapExpression<TSource, TDestination, TMember>> configure)
        {
            if (destinationMember == null)
            {
                throw new ArgumentNullException(nameof(destinationMember));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var property = GetProperty(destinationMember);
            var memberMap = _typeMap.GetOrAddMemberMap(property);
            configure(new MemberMapExpression<TSource, TDestination, TMember>(memberMap));
            return this;
        }

        private static PropertyInfo GetProperty<TMember>(Expression<Func<TDestination, TMember>> expression)
        {
            if (expression.Body is MemberExpression member && member.Member is PropertyInfo property)
            {
                return property;
            }

            throw new InvalidOperationException("The destination member expression must target a property.");
        }
    }
}
