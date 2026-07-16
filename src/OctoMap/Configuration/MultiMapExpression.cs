namespace OctoMap.Configuration
{
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Represents an internal multi-source type map expression.
    /// </summary>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    internal sealed class MultiMapExpression<TDestination> : IMultiMapExpression<TDestination>
    {
        private readonly MultiSourceTypeMap _typeMap;

        /// <summary>
        /// Initializes a new instance of the <see cref="MultiMapExpression{TDestination}"/> class.
        /// </summary>
        /// <param name="typeMap">The multi-source type map.</param>
        public MultiMapExpression(MultiSourceTypeMap typeMap)
        {
            _typeMap = typeMap ?? throw new ArgumentNullException(nameof(typeMap));
        }

        /// <inheritdoc/>
        public IMultiMapExpression<TDestination> From<TSource>(Action<IMapExpression<TSource, TDestination>> configure)
        {
            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var sourceMap = _typeMap.AddSource(typeof(TSource));
            configure(new MapExpression<TSource, TDestination>(sourceMap));
            return this;
        }

        /// <inheritdoc/>
        public IMultiMapExpression<TDestination> ForMember<TMember>(
            Expression<Func<TDestination, TMember>> destinationMember,
            Action<IMultiSourceMemberMapExpression<TDestination, TMember>> configure)
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
            var memberMap = _typeMap.GetOrAddContextMemberMap(property);
            configure(new MultiSourceMemberMapExpression<TDestination, TMember>(memberMap));
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
