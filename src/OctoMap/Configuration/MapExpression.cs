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
        public IMapExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination, IMapContext> action)
            => AddLifecycleAction(LifecycleActionTiming.Before, action, null);

        /// <inheritdoc/>
        public IMapExpression<TSource, TDestination> BeforeMap<TAction>()
            where TAction : IMappingAction<TSource, TDestination>
            => AddLifecycleAction(LifecycleActionTiming.Before, null, typeof(TAction));

        /// <inheritdoc/>
        public IMapExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination, IMapContext> action)
            => AddLifecycleAction(LifecycleActionTiming.After, action, null);

        /// <inheritdoc/>
        public IMapExpression<TSource, TDestination> AfterMap<TAction>()
            where TAction : IMappingAction<TSource, TDestination>
            => AddLifecycleAction(LifecycleActionTiming.After, null, typeof(TAction));

        /// <inheritdoc/>
        public IMapExpression<TSource, TDestination> IncludeBase<TBaseSource, TBaseDestination>()
        {
            if (_configurationBuilder == null)
            {
                throw new NotSupportedException("IncludeBase requires a map created directly through CreateMap.");
            }

            if (!typeof(TBaseSource).IsAssignableFrom(typeof(TSource)))
            {
                throw new InvalidOperationException($"Base source type '{typeof(TBaseSource).FullName}' is not assignable from source type '{typeof(TSource).FullName}'.");
            }

            if (!typeof(TBaseDestination).IsAssignableFrom(typeof(TDestination)))
            {
                throw new InvalidOperationException($"Base destination type '{typeof(TBaseDestination).FullName}' is not assignable from destination type '{typeof(TDestination).FullName}'.");
            }

            if (!_configurationBuilder.TryGetMap(typeof(TBaseSource), typeof(TBaseDestination), out var baseMap))
            {
                throw new InvalidOperationException($"Base map '{typeof(TBaseSource).FullName}->{typeof(TBaseDestination).FullName}' is not configured.");
            }

            foreach (var memberMap in baseMap.MemberMaps.Values)
            {
                if (_typeMap.MemberMaps.ContainsKey(GetMemberMapKey(memberMap)))
                {
                    continue;
                }

                _typeMap.CopyMemberMap(memberMap);
            }

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

        private IMapExpression<TSource, TDestination> AddLifecycleAction(
            LifecycleActionTiming timing,
            Delegate inlineAction,
            Type actionType)
        {
            if (inlineAction == null && actionType == null)
            {
                throw new ArgumentNullException(nameof(inlineAction));
            }

            int? inlineActionId = inlineAction == null
                ? null
                : _configurationBuilder == null
                    ? throw new NotSupportedException("Inline lifecycle actions require a map created directly through CreateMap.")
                    : _configurationBuilder.AddInlineLifecycleAction((Action<TSource, TDestination, IMapContext>)inlineAction);
            _typeMap.AddLifecycleAction(new LifecycleActionMap(timing, inlineActionId, actionType));
            return this;
        }

        /// <inheritdoc/>
        public IMapExpression<TSource, TDestination> ForPath<TMember>(
            Expression<Func<TDestination, TMember>> destinationPath,
            Action<IMemberMapExpression<TSource, TDestination, TMember>> configure)
        {
            if (destinationPath == null)
            {
                throw new ArgumentNullException(nameof(destinationPath));
            }

            if (configure == null)
            {
                throw new ArgumentNullException(nameof(configure));
            }

            var path = GetPropertyPath(destinationPath);
            if (path.Count < 2)
            {
                throw new InvalidOperationException("The destination path expression must target a nested property path.");
            }

            var memberMap = _typeMap.GetOrAddMemberPathMap(path);
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

        private static IReadOnlyList<PropertyInfo> GetPropertyPath<TMember>(Expression<Func<TDestination, TMember>> expression)
        {
            var path = new Stack<PropertyInfo>();
            Expression current = expression.Body;
            while (current is MemberExpression member)
            {
                if (member.Member is not PropertyInfo property)
                {
                    throw new InvalidOperationException("The destination path expression must target properties only.");
                }

                path.Push(property);
                current = member.Expression;
            }

            if (current is not ParameterExpression parameter || !ReferenceEquals(parameter, expression.Parameters[0]))
            {
                throw new InvalidOperationException("The destination path expression must start from the destination parameter.");
            }

            return path.ToArray();
        }

        private void CopyReversibleMemberMaps(TypeMap reverseMap)
        {
            foreach (var memberMap in _typeMap.MemberMaps.Values)
            {
                if (TryGetDirectSourceProperty(memberMap.SourceExpression, out var sourceProperty))
                {
                    var reverseSourceProperty = memberMap.UsesDestinationPath
                        ? memberMap.DestinationPath[0]
                        : _typeMap.DestinationType.GetProperty(memberMap.DestinationProperty.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
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
                    reverseMemberMap.SourceExpression = memberMap.UsesDestinationPath
                        ? CreateSourcePropertyPathExpression(memberMap.DestinationPath)
                        : CreateSourcePropertyExpression(reverseSourceProperty);
                    reverseMemberMap.IsIgnored = false;
                    reverseMemberMap.HasConstantValue = false;
                    reverseMemberMap.ResolverType = null;
                    reverseMemberMap.ConverterType = null;
                    reverseMemberMap.ConverterSourceExpression = null;
                    reverseMemberMap.AllowNullCollection = memberMap.AllowNullCollection;
                    reverseMemberMap.IgnoreNullSourceValue = memberMap.IgnoreNullSourceValue;
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

        private static LambdaExpression CreateSourcePropertyPathExpression(IReadOnlyList<PropertyInfo> sourcePath)
        {
            var parameter = Expression.Parameter(typeof(TDestination), "source");
            Expression current = parameter;
            foreach (var property in sourcePath)
            {
                current = Expression.Property(current, property);
            }

            return Expression.Lambda(current, parameter);
        }

        private static string GetMemberMapKey(MemberMap memberMap)
            => memberMap.UsesDestinationPath
                ? string.Join(".", memberMap.DestinationPath.Select(x => x.Name))
                : memberMap.DestinationProperty.Name;
    }
}
