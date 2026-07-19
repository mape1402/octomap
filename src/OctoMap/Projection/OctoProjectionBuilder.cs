using System.Linq.Expressions;
using System.Reflection;
using OctoMap.Configuration;
using OctoMap.Planning;
using OctoMap.Validation;

namespace OctoMap.Projection
{
    /// <summary>
    /// Builds LINQ projection expressions from OctoMap mapping plans.
    /// </summary>
    internal sealed class OctoProjectionBuilder : IOctoProjectionBuilder
    {
        private readonly IOctoMapConfiguration _configuration;
        private readonly OctoMapOptions _options;
        private readonly IMappingPlanBuilder _planBuilder;
        private readonly IOctoMapValidator _validator;

        /// <summary>
        /// Initializes a new instance of the <see cref="OctoProjectionBuilder"/> class.
        /// </summary>
        /// <param name="configuration">The map configuration.</param>
        /// <param name="options">The OctoMap options.</param>
        /// <param name="planBuilder">The mapping plan builder.</param>
        /// <param name="validator">The configuration validator.</param>
        public OctoProjectionBuilder(
            IOctoMapConfiguration configuration,
            OctoMapOptions options,
            IMappingPlanBuilder planBuilder,
            IOctoMapValidator validator)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _planBuilder = planBuilder ?? throw new ArgumentNullException(nameof(planBuilder));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        /// <inheritdoc/>
        public Expression<Func<TSource, TDestination>> Build<TSource, TDestination>()
            => (Expression<Func<TSource, TDestination>>)Build(typeof(TSource), typeof(TDestination));

        /// <inheritdoc/>
        public LambdaExpression Build(Type sourceType, Type destinationType)
        {
            if (sourceType == null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }

            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            var typeMap = _configuration.FindMap(sourceType, destinationType)
                ?? CreateImplicitMap(sourceType, destinationType);

            var report = _validator.Validate(new[] { typeMap });
            if (!report.IsValid)
            {
                throw new OctoMapValidationException(report);
            }

            var plan = _planBuilder.Build(typeMap);
            return BuildProjectionExpression(plan);
        }

        private static LambdaExpression BuildProjectionExpression(MappingPlan plan)
        {
            if (plan.SourceTypes.Count != 1)
            {
                throw new NotSupportedException("Projection is only supported for single-source maps.");
            }

            if (plan.LifecycleActions.Count > 0)
            {
                throw new NotSupportedException("Projection cannot use maps with lifecycle actions because they are runtime-only.");
            }

            var source = Expression.Parameter(plan.SourceType, "source");
            var construction = BuildDestinationConstruction(plan, source);
            var bindings = new List<MemberBinding>();
            foreach (var assignment in plan.Assignments)
            {
                if (assignment.UsesDestinationPath)
                {
                    throw new NotSupportedException($"Member path '{string.Join(".", assignment.DestinationPath.Select(x => x.Name))}' cannot be projected because nested destination path projection is not supported yet.");
                }

                if (assignment.PreConditionExpression != null || assignment.ConditionExpression != null)
                {
                    throw new NotSupportedException($"Member '{assignment.DestinationProperty.Name}' cannot be projected because conditional mapping is not supported in projections yet.");
                }

                bindings.Add(Expression.Bind(assignment.DestinationProperty, BuildAssignmentExpression(plan, assignment, source)));
            }

            Expression body = bindings.Count == 0
                ? construction
                : Expression.MemberInit(construction, bindings);

            return Expression.Lambda(body, source);
        }

        private static NewExpression BuildDestinationConstruction(MappingPlan plan, ParameterExpression source)
        {
            if (plan.Construction?.ConstructionExpression != null)
            {
                var expression = ReplaceParameter(
                    plan.Construction.ConstructionExpression.Body,
                    plan.Construction.ConstructionExpression.Parameters[0],
                    source);
                if (expression is NewExpression explicitConstruction)
                {
                    return explicitConstruction;
                }

                throw new NotSupportedException("Projectable ConstructUsing expressions must create the destination directly with a constructor expression.");
            }

            if (plan.Construction?.Constructor != null)
            {
                var arguments = plan.Construction.Parameters
                    .Select<ConstructorParameterPlan, Expression>(parameter =>
                    {
                    Expression value = Expression.Property(source, parameter.SourceProperty);
                    if (parameter.TypeConversion != null)
                    {
                        value = ApplyTypeConversion(value, parameter.TypeConversion, parameter.Parameter.Name);
                    }

                    return value.Type == parameter.Parameter.ParameterType
                        ? value
                        : Expression.Convert(value, parameter.Parameter.ParameterType);
                    });

                return Expression.New(plan.Construction.Constructor, arguments);
            }

            var defaultConstructor = plan.DestinationType.GetConstructor(Type.EmptyTypes);
            if (defaultConstructor == null)
            {
                throw new NotSupportedException($"Destination type '{plan.DestinationType.FullName}' cannot be projected because it does not expose a supported constructor.");
            }

            return Expression.New(defaultConstructor);
        }

        private static Expression BuildAssignmentExpression(MappingPlan plan, MemberAssignmentPlan assignment, ParameterExpression source)
        {
            if (assignment.ResolverType != null)
            {
                throw new NotSupportedException($"Member '{assignment.DestinationProperty.Name}' cannot be projected because resolvers are runtime-only.");
            }

            if (assignment.ConverterType != null)
            {
                throw new NotSupportedException($"Member '{assignment.DestinationProperty.Name}' cannot be projected because DI value converters are runtime-only.");
            }

            if (assignment.UseNestedMap)
            {
                throw new NotSupportedException($"Member '{assignment.DestinationProperty.Name}' cannot be projected because nested projection is not supported yet.");
            }

            if (assignment.UseCollectionMap)
            {
                return BuildCollectionExpression(assignment, source);
            }

            Expression value;
            if (assignment.HasConstantValue)
            {
                value = Expression.Constant(assignment.ConstantValue, assignment.DestinationProperty.PropertyType);
            }
            else if (assignment.SourceExpression != null)
            {
                value = ReplaceParameter(
                    assignment.SourceExpression.Body,
                    assignment.SourceExpression.Parameters[0],
                    source);
            }
            else if (assignment.UseFlattenedMap)
            {
                value = BuildFlattenedExpression(assignment, source);
            }
            else if (assignment.SourceProperty != null)
            {
                value = Expression.Property(source, assignment.SourceProperty);
            }
            else
            {
                throw new NotSupportedException($"Member '{assignment.DestinationProperty.Name}' does not have a projectable source expression.");
            }

            if (assignment.HasNullSubstitute)
            {
                value = Expression.Coalesce(value, Expression.Constant(assignment.NullSubstitute, value.Type));
            }

            if (assignment.TypeConversion != null)
            {
                value = ApplyTypeConversion(value, assignment.TypeConversion, assignment.DestinationProperty.Name);
            }

            return value.Type == assignment.DestinationProperty.PropertyType
                ? value
                : Expression.Convert(value, assignment.DestinationProperty.PropertyType);
        }

        private static Expression BuildCollectionExpression(MemberAssignmentPlan assignment, ParameterExpression source)
        {
            var sourceCollection = Expression.Property(source, assignment.SourceProperty);
            var sourceItem = Expression.Parameter(assignment.SourceElementType, "item");
            var destinationItem = BuildCollectionItemExpression(assignment, sourceItem);
            var selector = Expression.Lambda(destinationItem, sourceItem);
            var selected = Expression.Call(
                GetEnumerableSelectMethod(assignment.SourceElementType, assignment.DestinationElementType),
                sourceCollection,
                selector);

            var materialized = MaterializeCollectionExpression(assignment, selected);
            var mapped = materialized.Type == assignment.DestinationProperty.PropertyType
                ? materialized
                : Expression.Convert(materialized, assignment.DestinationProperty.PropertyType);

            if (!CanBeNull(sourceCollection.Type))
            {
                return mapped;
            }

            return Expression.Condition(
                Expression.NotEqual(sourceCollection, Expression.Constant(null, sourceCollection.Type)),
                mapped,
                BuildNullCollectionExpression(assignment));
        }

        private static Expression BuildCollectionItemExpression(MemberAssignmentPlan assignment, ParameterExpression sourceItem)
        {
            if (assignment.DestinationElementType.IsAssignableFrom(assignment.SourceElementType))
            {
                return sourceItem.Type == assignment.DestinationElementType
                    ? sourceItem
                    : Expression.Convert(sourceItem, assignment.DestinationElementType);
            }

            if (assignment.ElementTypeConversion != null)
            {
                var converted = ApplyTypeConversion(sourceItem, assignment.ElementTypeConversion, assignment.DestinationProperty.Name);
                return converted.Type == assignment.DestinationElementType
                    ? converted
                    : Expression.Convert(converted, assignment.DestinationElementType);
            }

            throw new NotSupportedException($"Member '{assignment.DestinationProperty.Name}' cannot be projected because nested collection element projection is not supported yet.");
        }

        private static Expression MaterializeCollectionExpression(MemberAssignmentPlan assignment, Expression selected)
        {
            if (assignment.DestinationCollectionShape == CollectionShape.Array)
            {
                return Expression.Call(GetEnumerableToArrayMethod(assignment.DestinationElementType), selected);
            }

            if (assignment.DestinationCollectionShape == CollectionShape.Set)
            {
                return Expression.New(
                    GetHashSetEnumerableConstructor(assignment.DestinationElementType),
                    selected);
            }

            return Expression.Call(GetEnumerableToListMethod(assignment.DestinationElementType), selected);
        }

        private static Expression BuildNullCollectionExpression(MemberAssignmentPlan assignment)
        {
            if (assignment.AllowNullCollection || assignment.IgnoreNullSourceValue)
            {
                return Expression.Default(assignment.DestinationProperty.PropertyType);
            }

            Expression empty = assignment.DestinationCollectionShape switch
            {
                CollectionShape.Array => Expression.Call(GetEnumerableToArrayMethod(assignment.DestinationElementType), Expression.Call(GetEnumerableEmptyMethod(assignment.DestinationElementType))),
                CollectionShape.Set => Expression.New(typeof(HashSet<>).MakeGenericType(assignment.DestinationElementType)),
                _ => Expression.New(typeof(List<>).MakeGenericType(assignment.DestinationElementType))
            };

            return empty.Type == assignment.DestinationProperty.PropertyType
                ? empty
                : Expression.Convert(empty, assignment.DestinationProperty.PropertyType);
        }

        private static Expression BuildFlattenedExpression(MemberAssignmentPlan assignment, ParameterExpression source)
        {
            Expression current = source;
            var nullChecks = new List<Expression>();
            for (var index = 0; index < assignment.SourcePath.Count; index++)
            {
                current = Expression.Property(current, assignment.SourcePath[index]);
                if (index < assignment.SourcePath.Count - 1 && CanBeNull(current.Type))
                {
                    nullChecks.Add(Expression.NotEqual(current, Expression.Constant(null, current.Type)));
                }
            }

            if (assignment.TypeConversion != null)
            {
                current = ApplyTypeConversion(current, assignment.TypeConversion, assignment.DestinationProperty.Name);
            }

            var finalValue = current.Type == assignment.DestinationProperty.PropertyType
                ? current
                : Expression.Convert(current, assignment.DestinationProperty.PropertyType);

            if (nullChecks.Count == 0)
            {
                return finalValue;
            }

            var condition = nullChecks.Aggregate(Expression.AndAlso);
            return Expression.Condition(
                condition,
                finalValue,
                Expression.Default(assignment.DestinationProperty.PropertyType));
        }

        private ITypeMap CreateImplicitMap(Type sourceType, Type destinationType)
        {
            if (!_options.EnableRuntimeImplicitMaps)
            {
                throw new InvalidOperationException($"Projection map '{sourceType.FullName}->{destinationType.FullName}' is not configured and runtime implicit maps are disabled.");
            }

            return new TypeMap(sourceType, destinationType, true, _options, new Configuration.MapDeclaration("Runtime implicit projection map"));
        }

        private static Expression ReplaceParameter(Expression expression, ParameterExpression from, Expression to)
            => new ParameterReplacementVisitor(from, to).Visit(expression);

        private static Expression ApplyTypeConversion(Expression value, TypeConversionMap typeConversion, string memberName)
        {
            if (typeConversion.UsesServiceConverter)
            {
                throw new NotSupportedException($"Member '{memberName}' cannot be projected because DI type converters are runtime-only.");
            }

            return ReplaceParameter(typeConversion.ConversionExpression.Body, typeConversion.ConversionExpression.Parameters[0], value);
        }

        private static bool CanBeNull(Type type)
            => !type.IsValueType || Nullable.GetUnderlyingType(type) != null;

        private static MethodInfo GetEnumerableSelectMethod(Type sourceElementType, Type destinationElementType)
            => typeof(Enumerable)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(x => x.Name == nameof(Enumerable.Select)
                    && x.IsGenericMethodDefinition
                    && x.GetParameters().Length == 2
                    && x.GetParameters()[1].ParameterType.GetGenericTypeDefinition() == typeof(Func<,>))
                .MakeGenericMethod(sourceElementType, destinationElementType);

        private static MethodInfo GetEnumerableToArrayMethod(Type elementType)
            => typeof(Enumerable)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(x => x.Name == nameof(Enumerable.ToArray)
                    && x.IsGenericMethodDefinition
                    && x.GetParameters().Length == 1)
                .MakeGenericMethod(elementType);

        private static MethodInfo GetEnumerableToListMethod(Type elementType)
            => typeof(Enumerable)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(x => x.Name == nameof(Enumerable.ToList)
                    && x.IsGenericMethodDefinition
                    && x.GetParameters().Length == 1)
                .MakeGenericMethod(elementType);

        private static MethodInfo GetEnumerableEmptyMethod(Type elementType)
            => typeof(Enumerable)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(x => x.Name == nameof(Enumerable.Empty)
                    && x.IsGenericMethodDefinition)
                .MakeGenericMethod(elementType);

        private static ConstructorInfo GetHashSetEnumerableConstructor(Type elementType)
            => typeof(HashSet<>)
                .MakeGenericType(elementType)
                .GetConstructor(new[] { typeof(IEnumerable<>).MakeGenericType(elementType) });

        private sealed class ParameterReplacementVisitor : ExpressionVisitor
        {
            private readonly ParameterExpression _from;
            private readonly Expression _to;

            public ParameterReplacementVisitor(ParameterExpression from, Expression to)
            {
                _from = from;
                _to = to;
            }

            protected override Expression VisitParameter(ParameterExpression node)
                => ReferenceEquals(node, _from) ? _to : base.VisitParameter(node);
        }
    }
}
