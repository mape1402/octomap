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
        private readonly OctoMapConfigurationBuilder _configurationBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="MapExpression{TSource, TDestination}"/> class.
        /// </summary>
        /// <param name="typeMap">The type map.</param>
        /// <param name="configurationBuilder">The configuration builder.</param>
        public MapExpression(TypeMap typeMap, OctoMapConfigurationBuilder configurationBuilder = null)
        {
            _typeMap = typeMap ?? throw new ArgumentNullException(nameof(typeMap));
            _configurationBuilder = configurationBuilder;
        }

        /// <inheritdoc/>
        public IMapExpression<TDestination, TSource> ReverseMap()
        {
            if (_configurationBuilder == null)
            {
                throw new NotSupportedException("ReverseMap is only supported for single-source maps created directly through CreateMap.");
            }

            var reverseMap = _configurationBuilder.GetOrCreateMap(typeof(TDestination), typeof(TSource));
            CopyReversibleMemberMaps(reverseMap);
            return new MapExpression<TDestination, TSource>(reverseMap, _configurationBuilder);
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

        private void CopyReversibleMemberMaps(TypeMap reverseMap)
        {
            foreach (var memberMap in _typeMap.MemberMaps.Values)
            {
                if (TryGetDirectSourceProperty(memberMap.SourceExpression, out var sourceProperty))
                {
                    var reverseSourceProperty = _typeMap.DestinationType.GetProperty(memberMap.DestinationProperty.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                    if (reverseSourceProperty == null || !reverseSourceProperty.CanRead)
                    {
                        continue;
                    }

                    var reverseDestinationProperty = _typeMap.SourceType.GetProperty(sourceProperty.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                    if (reverseDestinationProperty == null)
                    {
                        continue;
                    }

                    var reverseMemberMap = reverseMap.GetOrAddMemberMap(reverseDestinationProperty);
                    reverseMemberMap.SourceExpression = CreateSourcePropertyExpression(reverseSourceProperty);
                    reverseMemberMap.IsIgnored = false;
                    reverseMemberMap.HasConstantValue = false;
                    reverseMemberMap.ResolverType = null;
                    reverseMemberMap.ConverterType = null;
                    reverseMemberMap.ConverterSourceExpression = null;
                }
            }
        }

        private static bool TryGetDirectSourceProperty(LambdaExpression expression, out PropertyInfo property)
        {
            property = null;
            if (expression == null)
            {
                return false;
            }

            var body = expression.Body;
            if (body is UnaryExpression unary
                && (unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked))
            {
                body = unary.Operand;
            }

            if (body is not MemberExpression member || member.Member is not PropertyInfo sourceProperty)
            {
                return false;
            }

            if (member.Expression is not ParameterExpression parameter || !ReferenceEquals(parameter, expression.Parameters[0]))
            {
                return false;
            }

            property = sourceProperty;
            return true;
        }

        private static LambdaExpression CreateSourcePropertyExpression(PropertyInfo sourceProperty)
        {
            var parameter = Expression.Parameter(typeof(TDestination), "source");
            return Expression.Lambda(Expression.Property(parameter, sourceProperty), parameter);
        }
    }
}
