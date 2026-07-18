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
                throw new NotSupportedException($"Member '{assignment.DestinationProperty.Name}' cannot be projected because collection projection is not supported yet.");
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

            return new TypeMap(sourceType, destinationType, true);
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
