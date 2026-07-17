using System.Linq.Expressions;
using System.Reflection;
using DynaBee.FluentApi;
using DynaBee.FluentApi.Body;
using DynaBee.FluentApi.DependencyInjection;
using DynaBee.FluentApi.Invocation;
using Microsoft.Extensions.DependencyInjection;
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
            var invoker = context.CreateBoundMethodInvoker(
                className,
                mapper,
                nameof(IOctoMapper<object, object>.Map),
                plan.SourceTypes.Concat(new[] { typeof(IMapContext) }).ToArray());
            return new CompiledMap(mapper, mapper.GetType(), invoker);
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
            var context = body.Parameter("context");

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
                var value = BuildAssignmentValue(body, sources, destination, context, assignment);
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
            IBeeValueExpression destination,
            IBeeValueExpression context,
            MemberAssignmentPlan assignment)
        {
            IBeeValueExpression value;
            if (assignment.ConverterType != null)
            {
                value = BuildConverterValue(body, sources, context, assignment);
            }
            else if (assignment.ResolverType != null)
            {
                value = BuildResolverValue(body, sources, destination, context, assignment);
            }
            else if (assignment.HasConstantValue)
            {
                value = body.Constant(assignment.ConstantValue, assignment.DestinationProperty.PropertyType);
            }
            else if (assignment.SourceExpression != null)
            {
                value = BuildExpression(body, sources, assignment.SourceExpression.Body, assignment.SourceExpression.Parameters[0], assignment.SourceIndex);
            }
            else if (assignment.UseCollectionMap)
            {
                value = BuildCollectionMapValue(body, sources, context, assignment);
            }
            else if (assignment.UseNestedMap)
            {
                value = BuildNestedMapValue(body, sources, context, assignment);
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

        private static IBeeValueExpression BuildResolverValue(
            IBeeMethodBodyBuilder body,
            IReadOnlyList<IBeeValueExpression> sources,
            IBeeValueExpression destination,
            IBeeValueExpression context,
            MemberAssignmentPlan assignment)
        {
            if (assignment.SourceIndex < 0)
            {
                throw new NotSupportedException("Context-level multi-source resolvers are not supported yet.");
            }

            var source = sources[assignment.SourceIndex];
            var services = body.Property(context, nameof(IMapContext.Services));
            var resolver = body.StaticCall(GetRequiredServiceMethod(assignment.ResolverType), services);
            var resolverContract = typeof(IValueResolver<,,>).MakeGenericType(
                source.Type,
                destination.Type,
                assignment.DestinationProperty.PropertyType);
            var resolveMethod = resolverContract.GetMethod(nameof(IValueResolver<object, object, object>.Resolve));

            return body.Call(resolver, resolveMethod, source, destination, context);
        }

        private static IBeeValueExpression BuildConverterValue(
            IBeeMethodBodyBuilder body,
            IReadOnlyList<IBeeValueExpression> sources,
            IBeeValueExpression context,
            MemberAssignmentPlan assignment)
        {
            var sourceValue = BuildExpression(
                body,
                sources,
                assignment.ConverterSourceExpression.Body,
                assignment.ConverterSourceExpression.Parameters[0],
                assignment.SourceIndex);
            var services = body.Property(context, nameof(IMapContext.Services));
            var converter = body.StaticCall(GetRequiredServiceMethod(assignment.ConverterType), services);
            var converterContract = typeof(IValueConverter<,>).MakeGenericType(
                sourceValue.Type,
                assignment.DestinationProperty.PropertyType);
            var convertMethod = converterContract.GetMethod(nameof(IValueConverter<object, object>.Convert));

            return body.Call(converter, convertMethod, sourceValue, context);
        }

        private static IBeeValueExpression BuildCollectionMapValue(
            IBeeMethodBodyBuilder body,
            IReadOnlyList<IBeeValueExpression> sources,
            IBeeValueExpression context,
            MemberAssignmentPlan assignment)
        {
            var source = sources[assignment.SourceIndex];
            var sourceCollection = body.Property(source, assignment.SourceProperty.Name);
            var destinationCollection = body.DeclareLocal($"collection_{assignment.DestinationProperty.Name}", assignment.DestinationProperty.PropertyType);

            body.If(
                body.IsNull(sourceCollection),
                whenTrue => whenTrue.Assign(destinationCollection, body.Default(assignment.DestinationProperty.PropertyType)),
                whenFalse =>
                {
                    var count = BuildCollectionCountValue(whenFalse, sourceCollection, assignment.SourceCollectionShape);
                    whenFalse.Assign(destinationCollection, CreateDestinationCollection(whenFalse, assignment, count));

                    var index = whenFalse.DeclareLocal($"index_{assignment.DestinationProperty.Name}", typeof(int));
                    whenFalse.For(
                        initialize: loop => loop.Assign(index, loop.Constant(0)),
                        condition: loop => loop.LessThan(index, BuildCollectionCountValue(loop, sourceCollection, assignment.SourceCollectionShape)),
                        increment: loop => loop.Assign(index, loop.Add(index, loop.Constant(1))),
                        body: loop =>
                        {
                            var sourceItem = loop.Index(sourceCollection, index);
                            var destinationItem = BuildCollectionItemValue(loop, sourceItem, context, assignment);
                            AssignCollectionItem(loop, destinationCollection, index, destinationItem, assignment);
                        });
                });

            return destinationCollection;
        }

        private static IBeeValueExpression BuildCollectionCountValue(
            IBeeMethodBodyBuilder body,
            IBeeValueExpression collection,
            CollectionShape shape)
            => shape == CollectionShape.Array
                ? body.Property(collection, nameof(Array.Length))
                : body.Property(collection, nameof(List<object>.Count));

        private static IBeeValueExpression CreateDestinationCollection(
            IBeeMethodBodyBuilder body,
            MemberAssignmentPlan assignment,
            IBeeValueExpression count)
            => assignment.DestinationCollectionShape == CollectionShape.Array
                ? body.NewArray(assignment.DestinationElementType, count)
                : body.New(assignment.DestinationProperty.PropertyType, count);

        private static IBeeValueExpression BuildCollectionItemValue(
            IBeeMethodBodyBuilder body,
            IBeeValueExpression sourceItem,
            IBeeValueExpression context,
            MemberAssignmentPlan assignment)
        {
            if (assignment.DestinationElementType.IsAssignableFrom(assignment.SourceElementType))
            {
                return sourceItem.Type == assignment.DestinationElementType
                    ? sourceItem
                    : body.Convert(sourceItem, assignment.DestinationElementType);
            }

            var services = body.Property(context, nameof(IMapContext.Services));
            var mapper = body.StaticCall(GetRequiredServiceMethod(typeof(IOctoMapper)), services);
            var mapMethod = typeof(IOctoMapper)
                .GetMethods()
                .Single(x => x.Name == nameof(IOctoMapper.Map)
                    && x.IsGenericMethodDefinition
                    && x.GetGenericArguments().Length == 2)
                .MakeGenericMethod(assignment.SourceElementType, assignment.DestinationElementType);

            return body.Call(mapper, mapMethod, sourceItem);
        }

        private static void AssignCollectionItem(
            IBeeMethodBodyBuilder body,
            IBeeValueExpression destinationCollection,
            IBeeValueExpression index,
            IBeeValueExpression destinationItem,
            MemberAssignmentPlan assignment)
        {
            if (assignment.DestinationCollectionShape == CollectionShape.Array)
            {
                body.Assign(body.Index(destinationCollection, index), destinationItem);
                return;
            }

            var addMethod = assignment.DestinationProperty.PropertyType.GetMethod(
                nameof(List<object>.Add),
                new[] { assignment.DestinationElementType });
            body.Evaluate(body.Call(destinationCollection, addMethod, destinationItem));
        }

        private static IBeeValueExpression BuildNestedMapValue(
            IBeeMethodBodyBuilder body,
            IReadOnlyList<IBeeValueExpression> sources,
            IBeeValueExpression context,
            MemberAssignmentPlan assignment)
        {
            var source = sources[assignment.SourceIndex];
            var sourceValue = body.Property(source, assignment.SourceProperty.Name);
            var services = body.Property(context, nameof(IMapContext.Services));
            var mapper = body.StaticCall(GetRequiredServiceMethod(typeof(IOctoMapper)), services);
            var mapMethod = typeof(IOctoMapper)
                .GetMethods()
                .Single(x => x.Name == nameof(IOctoMapper.Map)
                    && x.IsGenericMethodDefinition
                    && x.GetGenericArguments().Length == 2)
                .MakeGenericMethod(sourceValue.Type, assignment.DestinationProperty.PropertyType);
            var mappedValue = body.Call(mapper, mapMethod, sourceValue);

            return body.If(
                body.IsNull(sourceValue),
                body.Default(assignment.DestinationProperty.PropertyType),
                mappedValue);
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
                    throw new NotSupportedException($"Expression node '{expression.NodeType}' is not supported by the current OctoMap expression generator.");
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

            throw new NotSupportedException($"Member '{expression.Member.Name}' is not supported by the current OctoMap expression generator.");
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
                    throw new NotSupportedException($"Binary expression '{expression.NodeType}' is not supported by the current OctoMap expression generator.");
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

            throw new NotSupportedException($"Method call '{expression.Method.Name}' is not supported by the current OctoMap multi-source expression generator.");
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

        private static MethodInfo GetRequiredServiceMethod(Type serviceType)
            => typeof(ServiceProviderServiceExtensions)
                .GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Single(x => x.Name == nameof(ServiceProviderServiceExtensions.GetRequiredService)
                    && x.IsGenericMethodDefinition
                    && x.GetParameters().Length == 1
                    && x.GetParameters()[0].ParameterType == typeof(IServiceProvider))
                .MakeGenericMethod(serviceType);

    }
}
