using System.Linq.Expressions;
using System.Reflection;
using DynaBee.FluentApi;
using DynaBee.FluentApi.Body;
using DynaBee.FluentApi.DependencyInjection;
using OctoMap.Planning;

namespace OctoMap.Generation.Dynabee
{
    /// <summary>
    /// Compiles mapping plans using DynaBee runtime type generation.
    /// </summary>
    internal sealed class DynabeeMappingGenerationBackend : IMappingGenerationBackend
    {
        private readonly IDynaBeeAssemblyBuilderFactory _builderFactory;
        private int _sequence;

        /// <summary>
        /// Initializes a new instance of the <see cref="DynabeeMappingGenerationBackend"/> class.
        /// </summary>
        /// <param name="builderFactory">The DynaBee assembly builder factory.</param>
        public DynabeeMappingGenerationBackend(IDynaBeeAssemblyBuilderFactory builderFactory)
        {
            _builderFactory = builderFactory ?? throw new ArgumentNullException(nameof(builderFactory));
        }

        /// <inheritdoc/>
        public string Name => "Dynabee";

        /// <inheritdoc/>
        public bool Supports(MappingPlan plan)
            => plan != null;

        /// <inheritdoc/>
        public CompiledMap Compile(MappingPlan plan)
        {
            if (plan == null)
            {
                throw new ArgumentNullException(nameof(plan));
            }

            var className = BuildClassName(plan);
            var context = _builderFactory
                .Create($"OctoMap.Generated.{Interlocked.Increment(ref _sequence)}")
                .DisableCache()
                .AddClass(className, c =>
                {
                    c.RegisterAsConcrete(false);
                    if (plan.SourceTypes.Count == 1)
                    {
                        c.Implements(typeof(IOctoMapper<,>).MakeGenericType(plan.SourceType, plan.DestinationType), false);
                    }

                    c.AddMethod(nameof(IOctoMapper<object, object>.Map), plan.DestinationType, m =>
                    {
                        for (var index = 0; index < plan.SourceTypes.Count; index++)
                        {
                            m.WithParameter(GetSourceParameterName(plan, index), plan.SourceTypes[index]);
                        }

                        m.WithParameter<IMapContext>("context")
                            .EmitsBody(body => EmitMapMethod(body, plan));
                    });
                })
                .Build();

            var mapper = context.CreateInstance(className);
            var sourceSetInvoker = plan.SourceTypes.Count > 1
                ? CreateSourceSetInvoker(mapper, mapper.GetType(), plan)
                : null;
            return new CompiledMap(mapper, mapper.GetType(), sourceSetInvoker);
        }

        private static string BuildClassName(MappingPlan plan)
        {
            var sourceName = Sanitize(plan.SourceType.Name);
            var destinationName = Sanitize(plan.DestinationType.Name);
            return $"{sourceName}To{destinationName}Mapper";
        }

        private static string Sanitize(string value)
        {
            var chars = value.Select(x => char.IsLetterOrDigit(x) ? x : '_').ToArray();
            return new string(chars);
        }

        private static void EmitMapMethod(IBeeMethodBodyBuilder body, MappingPlan plan)
        {
            var sources = plan.SourceTypes
                .Select((_, index) => body.Parameter(GetSourceParameterName(plan, index)))
                .ToArray();
            var destination = body.DeclareLocal("destination", plan.DestinationType);

            if (plan.SourceTypes.Count == 1 && !plan.SourceType.IsValueType)
            {
                body.If(
                    body.IsNull(sources[0]),
                    whenTrue => whenTrue.Return(body.Default(plan.DestinationType)));
            }

            body.Assign(destination, CreateDestination(body, plan.DestinationType));

            foreach (var assignment in plan.Assignments)
            {
                var target = body.Property(destination, assignment.DestinationProperty.Name);
                var value = BuildAssignmentValue(body, sources, assignment);
                body.Assign(target, value);
            }

            body.Return(destination);
        }

        private static IBeeValueExpression CreateDestination(IBeeMethodBodyBuilder body, Type destinationType)
        {
            return destinationType.IsValueType
                ? body.Default(destinationType)
                : body.New(destinationType);
        }

        private static IBeeValueExpression BuildAssignmentValue(
            IBeeMethodBodyBuilder body,
            IReadOnlyList<IBeeValueExpression> sources,
            MemberAssignmentPlan assignment)
        {
            IBeeValueExpression value;
            if (assignment.HasConstantValue)
            {
                value = body.Constant(assignment.ConstantValue, assignment.DestinationProperty.PropertyType);
            }
            else if (assignment.SourceExpression != null)
            {
                value = BuildExpression(body, sources, assignment.SourceExpression.Body, assignment.SourceExpression.Parameters[0], assignment.SourceIndex);
            }
            else
            {
                var source = sources[assignment.SourceIndex];
                value = body.Property(source, assignment.SourceProperty.Name);
            }

            if (assignment.HasNullSubstitute)
            {
                value = ApplyNullSubstitute(body, value, assignment);
            }

            return value.Type == assignment.DestinationProperty.PropertyType
                ? value
                : body.Convert(value, assignment.DestinationProperty.PropertyType);
        }

        private static IBeeValueExpression BuildExpression(
            IBeeMethodBodyBuilder body,
            IReadOnlyList<IBeeValueExpression> sources,
            Expression expression,
            ParameterExpression sourceParameter,
            int sourceIndex)
        {
            switch (expression)
            {
                case ParameterExpression parameter when ReferenceEquals(parameter, sourceParameter):
                    return sources[sourceIndex];
                case MemberExpression member:
                    return BuildMemberExpression(body, sources, member, sourceParameter, sourceIndex);
                case ConstantExpression constant:
                    return body.Constant(constant.Value, constant.Type);
                case BinaryExpression binary:
                    return BuildBinaryExpression(body, sources, binary, sourceParameter, sourceIndex);
                case UnaryExpression unary when unary.NodeType == ExpressionType.Convert || unary.NodeType == ExpressionType.ConvertChecked:
                    return body.Convert(BuildExpression(body, sources, unary.Operand, sourceParameter, sourceIndex), unary.Type);
                case ConditionalExpression conditional:
                    return body.If(
                        BuildExpression(body, sources, conditional.Test, sourceParameter, sourceIndex),
                        BuildExpression(body, sources, conditional.IfTrue, sourceParameter, sourceIndex),
                        BuildExpression(body, sources, conditional.IfFalse, sourceParameter, sourceIndex));
                case MethodCallExpression call:
                    return BuildMethodCallExpression(sources, call, sourceParameter);
                default:
                    throw new NotSupportedException($"Expression node '{expression.NodeType}' is not supported by OctoMap Phase 2.");
            }
        }

        private static IBeeValueExpression BuildMemberExpression(
            IBeeMethodBodyBuilder body,
            IReadOnlyList<IBeeValueExpression> sources,
            MemberExpression expression,
            ParameterExpression sourceParameter,
            int sourceIndex)
        {
            if (expression.Member is PropertyInfo property)
            {
                if (expression.Expression == null)
                {
                    return body.StaticProperty(property.DeclaringType, property.Name);
                }

                return body.Property(BuildExpression(body, sources, expression.Expression, sourceParameter, sourceIndex), property.Name);
            }

            if (expression.Member is FieldInfo field)
            {
                if (expression.Expression == null)
                {
                    return body.StaticField(field.DeclaringType, field.Name);
                }

                return body.Field(BuildExpression(body, sources, expression.Expression, sourceParameter, sourceIndex), field.Name);
            }

            throw new NotSupportedException($"Member '{expression.Member.Name}' is not supported by OctoMap Phase 2.");
        }

        private static IBeeValueExpression BuildBinaryExpression(
            IBeeMethodBodyBuilder body,
            IReadOnlyList<IBeeValueExpression> sources,
            BinaryExpression expression,
            ParameterExpression sourceParameter,
            int sourceIndex)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.Add:
                    return body.Add(
                        BuildExpression(body, sources, expression.Left, sourceParameter, sourceIndex),
                        BuildExpression(body, sources, expression.Right, sourceParameter, sourceIndex));
                case ExpressionType.Equal:
                    return body.Equal(
                        BuildExpression(body, sources, expression.Left, sourceParameter, sourceIndex),
                        BuildExpression(body, sources, expression.Right, sourceParameter, sourceIndex));
                case ExpressionType.NotEqual:
                    return body.NotEqual(
                        BuildExpression(body, sources, expression.Left, sourceParameter, sourceIndex),
                        BuildExpression(body, sources, expression.Right, sourceParameter, sourceIndex));
                default:
                    throw new NotSupportedException($"Binary expression '{expression.NodeType}' is not supported by OctoMap Phase 2.");
            }
        }

        private static IBeeValueExpression BuildMethodCallExpression(
            IReadOnlyList<IBeeValueExpression> sources,
            MethodCallExpression expression,
            ParameterExpression sourceParameter)
        {
            if (ReferenceEquals(expression.Object, sourceParameter)
                && expression.Method.IsGenericMethod
                && expression.Method.GetGenericMethodDefinition() == typeof(IMultiSourceMapContext).GetMethod(nameof(IMultiSourceMapContext.Get)))
            {
                var requestedType = expression.Method.GetGenericArguments()[0];
                for (var index = 0; index < sources.Count; index++)
                {
                    if (requestedType.IsAssignableFrom(sources[index].Type))
                    {
                        return sources[index];
                    }
                }
            }

            throw new NotSupportedException($"Method call '{expression.Method.Name}' is not supported by OctoMap Phase 3.");
        }

        private static IBeeValueExpression ApplyNullSubstitute(
            IBeeMethodBodyBuilder body,
            IBeeValueExpression value,
            MemberAssignmentPlan assignment)
        {
            if (assignment.DestinationProperty.PropertyType.IsValueType)
            {
                throw new NotSupportedException("NullSubstitute for value types is not supported yet.");
            }

            return body.If(
                body.IsNull(value),
                body.Constant(assignment.NullSubstitute, assignment.DestinationProperty.PropertyType),
                value);
        }

        private static string GetSourceParameterName(MappingPlan plan, int index)
            => plan.SourceTypes.Count == 1 ? "source" : $"source{index}";

        private static Func<SourceSet, IMapContext, object> CreateSourceSetInvoker(object mapper, Type mapperType, MappingPlan plan)
        {
            var mapperInstance = Expression.Constant(mapper, mapperType);
            var sourceSet = Expression.Parameter(typeof(SourceSet), "sources");
            var context = Expression.Parameter(typeof(IMapContext), "context");
            var getSourceMethod = typeof(SourceSet).GetMethod(nameof(SourceSet.Get));
            var arguments = plan.SourceTypes
                .Select(sourceType => Expression.Call(sourceSet, getSourceMethod.MakeGenericMethod(sourceType)))
                .Concat(new Expression[] { context })
                .ToArray();
            var method = mapperType.GetMethod(nameof(IOctoMapper<object, object>.Map), plan.SourceTypes.Concat(new[] { typeof(IMapContext) }).ToArray());
            var call = Expression.Call(mapperInstance, method, arguments);
            return Expression.Lambda<Func<SourceSet, IMapContext, object>>(
                Expression.Convert(call, typeof(object)),
                sourceSet,
                context).Compile();
        }
    }
}
