using System.Reflection;
using OctoMap.Configuration;

namespace OctoMap.Planning
{
    /// <summary>
    /// Builds convention-based mapping plans.
    /// </summary>
    internal sealed class ConventionMappingPlanBuilder : IMappingPlanBuilder
    {
        private readonly OctoMapOptions _options;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConventionMappingPlanBuilder"/> class.
        /// </summary>
        /// <param name="options">The OctoMap options.</param>
        public ConventionMappingPlanBuilder(OctoMapOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        /// <inheritdoc/>
        public MappingPlan Build(ITypeMap typeMap)
        {
            if (typeMap == null)
            {
                throw new ArgumentNullException(nameof(typeMap));
            }

            if (typeMap is MultiSourceTypeMap multiSourceTypeMap)
            {
                return BuildMultiSource(multiSourceTypeMap);
            }

            var sourceProperties = typeMap.SourceType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.CanRead && x.GetMethod != null)
                .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            var explicitMemberMaps = typeMap is TypeMap configuredTypeMap
                ? configuredTypeMap.MemberMaps
                : new Dictionary<string, MemberMap>(StringComparer.OrdinalIgnoreCase);
            var explicitRootMemberMaps = explicitMemberMaps.Values
                .Where(x => !x.UsesDestinationPath)
                .ToDictionary(x => x.DestinationProperty.Name, StringComparer.OrdinalIgnoreCase);
            var explicitPathMemberMaps = explicitMemberMaps.Values
                .Where(x => x.UsesDestinationPath)
                .ToArray();
            var pathRootNames = new HashSet<string>(
                explicitPathMemberMaps.Select(x => x.DestinationPath[0].Name),
                StringComparer.OrdinalIgnoreCase);

            var configuredConstructionExpression = typeMap is TypeMap configuredConstructionTypeMap
                ? configuredConstructionTypeMap.ConstructionExpression
                : null;
            var construction = CreateConstructionPlan(typeMap.SourceType, typeMap.DestinationType, sourceProperties, configuredConstructionExpression);

            var assignments = new List<MemberAssignmentPlan>();
            foreach (var destinationProperty in typeMap.DestinationType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (construction?.ConstructedMemberNames.Contains(destinationProperty.Name) == true)
                {
                    continue;
                }

                if (!CanWrite(destinationProperty))
                {
                    continue;
                }

                if (pathRootNames.Contains(destinationProperty.Name))
                {
                    continue;
                }

                if (explicitRootMemberMaps.TryGetValue(destinationProperty.Name, out var memberMap))
                {
                    if (memberMap.IsIgnored)
                    {
                        continue;
                    }

                    if (memberMap.ConverterType != null)
                    {
                        assignments.Add(CreateAssignment(destinationProperty, null, null, memberMap));
                        continue;
                    }

                    if (memberMap.ResolverType != null)
                    {
                        assignments.Add(CreateAssignment(destinationProperty, null, null, memberMap));
                        continue;
                    }

                    if (memberMap.SourceExpression != null)
                    {
                        assignments.Add(CreateAssignment(destinationProperty, null, memberMap.SourceExpression, memberMap));
                        continue;
                    }

                    if (memberMap.HasConstantValue)
                    {
                        assignments.Add(CreateAssignment(destinationProperty, null, null, memberMap));
                        continue;
                    }
                }

                if (!sourceProperties.TryGetValue(destinationProperty.Name, out var sourceProperty))
                {
                    if (TryCreateFlattenedAssignment(destinationProperty, sourceProperties, out var flattenedAssignment))
                    {
                        assignments.Add(flattenedAssignment);
                    }

                    continue;
                }

                explicitRootMemberMaps.TryGetValue(destinationProperty.Name, out var configuredMemberMap);
                if (TryCreateCollectionAssignment(destinationProperty, sourceProperty, configuredMemberMap, out var collectionAssignment))
                {
                    assignments.Add(collectionAssignment);
                    continue;
                }

                if (!destinationProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
                {
                    if (CanUseNestedMap(sourceProperty.PropertyType, destinationProperty.PropertyType))
                    {
                        assignments.Add(new MemberAssignmentPlan(
                            destinationProperty,
                            sourceProperty,
                            null,
                            null,
                            null,
                            null,
                            true,
                            CollectionShape.None,
                            CollectionShape.None,
                            null,
                            null,
                            true,
                            false,
                            null,
                            false,
                            null,
                            0));
                    }

                    continue;
                }

                assignments.Add(CreateAssignment(destinationProperty, sourceProperty, null, memberMap));
            }

            foreach (var memberMap in explicitPathMemberMaps)
            {
                if (memberMap.IsIgnored)
                {
                    continue;
                }

                assignments.Add(CreateAssignment(memberMap.DestinationProperty, null, memberMap.SourceExpression, memberMap));
            }

            return new MappingPlan(typeMap.SourceType, typeMap.DestinationType, assignments, construction);
        }

        private static bool CanWrite(PropertyInfo property)
            => property.CanWrite && property.SetMethod != null && property.SetMethod.IsPublic;

        private static MemberAssignmentPlan CreateAssignment(
            PropertyInfo destinationProperty,
            PropertyInfo sourceProperty,
            System.Linq.Expressions.LambdaExpression sourceExpression,
            MemberMap memberMap)
            => new(
                destinationProperty,
                sourceProperty,
                sourceExpression,
                memberMap?.ResolverType,
                memberMap?.ConverterType,
                memberMap?.ConverterSourceExpression,
                false,
                CollectionShape.None,
                CollectionShape.None,
                null,
                null,
                true,
                memberMap?.HasConstantValue == true,
                memberMap?.ConstantValue,
                memberMap?.HasNullSubstitute == true,
                memberMap?.NullSubstitute,
                0,
                null,
                memberMap?.DestinationPath);

        private static MappingPlan BuildMultiSource(MultiSourceTypeMap typeMap)
        {
            EnsureDestinationCanBeCreated(typeMap.DestinationType);

            if (typeMap.SourceMaps.Count == 0)
            {
                throw new InvalidOperationException($"Multi-source map for destination type '{typeMap.DestinationType.FullName}' must declare at least one source.");
            }

            var assignments = new List<MemberAssignmentPlan>();
            for (var sourceIndex = 0; sourceIndex < typeMap.SourceMaps.Count; sourceIndex++)
            {
                var sourceMap = typeMap.SourceMaps[sourceIndex];
                foreach (var memberMap in sourceMap.MemberMaps.Values)
                {
                    if (memberMap.IsIgnored)
                    {
                        continue;
                    }

                    memberMap.SourceIndex = sourceIndex;
                    assignments.Add(CreateAssignment(
                        memberMap.DestinationProperty,
                        null,
                        memberMap.SourceExpression,
                        memberMap,
                        sourceIndex));
                }
            }

            foreach (var memberMap in typeMap.ContextMemberMaps.Values)
            {
                assignments.Add(new MemberAssignmentPlan(
                    memberMap.DestinationProperty,
                    null,
                    memberMap.SourceExpression,
                    null,
                    null,
                    null,
                    false,
                    CollectionShape.None,
                    CollectionShape.None,
                    null,
                    null,
                    true,
                    false,
                    null,
                    false,
                    null,
                    -1));
            }

            return new MappingPlan(typeMap.SourceTypes, typeMap.DestinationType, assignments);
        }

        private static DestinationConstructionPlan CreateConstructionPlan(
            Type sourceType,
            Type destinationType,
            IReadOnlyDictionary<string, PropertyInfo> sourceProperties,
            System.Linq.Expressions.LambdaExpression constructionExpression)
        {
            EnsureDestinationCanBeCreated(sourceType, destinationType, sourceProperties, constructionExpression);

            if (constructionExpression != null)
            {
                var constructedMemberNames = GetConstructedMemberNames(constructionExpression);
                return new DestinationConstructionPlan(constructionExpression, null, null, constructedMemberNames);
            }

            if (destinationType.IsValueType || destinationType.GetConstructor(Type.EmptyTypes) != null)
            {
                return null;
            }

            if (!TrySelectConventionConstructor(destinationType, sourceProperties, out var constructor, out var parameterPlans))
            {
                return null;
            }

            return new DestinationConstructionPlan(
                null,
                constructor,
                parameterPlans,
                new HashSet<string>(parameterPlans.Select(x => x.Parameter.Name), StringComparer.OrdinalIgnoreCase));
        }

        private static MemberAssignmentPlan CreateAssignment(
            PropertyInfo destinationProperty,
            PropertyInfo sourceProperty,
            System.Linq.Expressions.LambdaExpression sourceExpression,
            MemberMap memberMap,
            int sourceIndex)
            => new(
                destinationProperty,
                sourceProperty,
                sourceExpression,
                resolverType: memberMap?.ResolverType,
                converterType: memberMap?.ConverterType,
                converterSourceExpression: memberMap?.ConverterSourceExpression,
                useNestedMap: false,
                sourceCollectionShape: CollectionShape.None,
                destinationCollectionShape: CollectionShape.None,
                sourceElementType: null,
                destinationElementType: null,
                allowNullCollection: true,
                memberMap?.HasConstantValue == true,
                memberMap?.ConstantValue,
                memberMap?.HasNullSubstitute == true,
                memberMap?.NullSubstitute,
                sourceIndex,
                null,
                memberMap?.DestinationPath);

        private static void EnsureDestinationCanBeCreated(Type destinationType)
        {
            if (destinationType.IsAbstract || destinationType.IsInterface)
            {
                throw new InvalidOperationException($"Destination type '{destinationType.FullName}' cannot be created.");
            }

            if (destinationType.IsValueType)
            {
                return;
            }

            if (destinationType.GetConstructor(Type.EmptyTypes) == null)
            {
                throw new InvalidOperationException($"Destination type '{destinationType.FullName}' must have a public parameterless constructor.");
            }
        }

        private static void EnsureDestinationCanBeCreated(
            Type sourceType,
            Type destinationType,
            IReadOnlyDictionary<string, PropertyInfo> sourceProperties,
            System.Linq.Expressions.LambdaExpression constructionExpression)
        {
            if (destinationType.IsAbstract || destinationType.IsInterface)
            {
                throw new InvalidOperationException($"Destination type '{destinationType.FullName}' cannot be created.");
            }

            if (destinationType.IsValueType || constructionExpression != null || destinationType.GetConstructor(Type.EmptyTypes) != null)
            {
                return;
            }

            if (!TrySelectConventionConstructor(destinationType, sourceProperties, out _, out _))
            {
                throw new InvalidOperationException($"Destination type '{destinationType.FullName}' must have a public parameterless constructor or constructor parameters that match readable source properties on '{sourceType.FullName}'.");
            }
        }

        private static bool TrySelectConventionConstructor(
            Type destinationType,
            IReadOnlyDictionary<string, PropertyInfo> sourceProperties,
            out ConstructorInfo constructor,
            out IReadOnlyList<ConstructorParameterPlan> parameterPlans)
        {
            foreach (var candidate in destinationType
                .GetConstructors(BindingFlags.Instance | BindingFlags.Public)
                .OrderByDescending(x => x.GetParameters().Length))
            {
                var parameters = candidate.GetParameters();
                if (parameters.Length == 0)
                {
                    continue;
                }

                var plans = new List<ConstructorParameterPlan>();
                var canUseConstructor = true;
                foreach (var parameter in parameters)
                {
                    if (!sourceProperties.TryGetValue(parameter.Name, out var sourceProperty)
                        || !parameter.ParameterType.IsAssignableFrom(sourceProperty.PropertyType))
                    {
                        canUseConstructor = false;
                        break;
                    }

                    plans.Add(new ConstructorParameterPlan(parameter, sourceProperty));
                }

                if (canUseConstructor)
                {
                    constructor = candidate;
                    parameterPlans = plans;
                    return true;
                }
            }

            constructor = null;
            parameterPlans = null;
            return false;
        }

        private static IReadOnlySet<string> GetConstructedMemberNames(System.Linq.Expressions.LambdaExpression constructionExpression)
        {
            if (constructionExpression.Body is not System.Linq.Expressions.NewExpression newExpression)
            {
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            if (newExpression.Members == null)
            {
                return new HashSet<string>(
                    newExpression.Constructor.GetParameters().Select(x => x.Name),
                    StringComparer.OrdinalIgnoreCase);
            }

            return new HashSet<string>(newExpression.Members.Select(x => x.Name), StringComparer.OrdinalIgnoreCase);
        }

        private static bool CanUseNestedMap(Type sourceType, Type destinationType)
        {
            if (sourceType == typeof(string) || destinationType == typeof(string))
            {
                return false;
            }

            if (sourceType.IsValueType || destinationType.IsValueType)
            {
                return false;
            }

            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(sourceType)
                || typeof(System.Collections.IEnumerable).IsAssignableFrom(destinationType))
            {
                return false;
            }

            return true;
        }

        private bool TryCreateCollectionAssignment(
            PropertyInfo destinationProperty,
            PropertyInfo sourceProperty,
            MemberMap memberMap,
            out MemberAssignmentPlan assignment)
        {
            assignment = null;
            if (!TryGetCollectionShape(sourceProperty.PropertyType, out var sourceShape, out var sourceElementType)
                || !TryGetCollectionShape(destinationProperty.PropertyType, out var destinationShape, out var destinationElementType))
            {
                return false;
            }

            assignment = new MemberAssignmentPlan(
                destinationProperty,
                sourceProperty,
                null,
                null,
                null,
                null,
                false,
                sourceShape,
                destinationShape,
                sourceElementType,
                destinationElementType,
                memberMap?.AllowNullCollection ?? _options.AllowNullCollections,
                false,
                null,
                false,
                null,
                0);
            return true;
        }

        private static bool TryCreateFlattenedAssignment(
            PropertyInfo destinationProperty,
            IReadOnlyDictionary<string, PropertyInfo> sourceProperties,
            out MemberAssignmentPlan assignment)
        {
            assignment = null;
            if (!TryResolveSourcePath(destinationProperty.Name, sourceProperties.Values, out var sourcePath))
            {
                return false;
            }

            var sourceValueType = sourcePath[^1].PropertyType;
            if (!destinationProperty.PropertyType.IsAssignableFrom(sourceValueType))
            {
                return false;
            }

            assignment = new MemberAssignmentPlan(
                destinationProperty,
                sourcePath[0],
                null,
                null,
                null,
                null,
                false,
                CollectionShape.None,
                CollectionShape.None,
                null,
                null,
                true,
                false,
                null,
                false,
                null,
                0,
                sourcePath);
            return true;
        }

        private static bool TryResolveSourcePath(
            string destinationName,
            IEnumerable<PropertyInfo> sourceProperties,
            out IReadOnlyList<PropertyInfo> sourcePath)
        {
            foreach (var sourceProperty in sourceProperties
                .Where(x => IsFlattenableSourceType(x.PropertyType))
                .OrderByDescending(x => x.Name.Length)
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                if (!destinationName.StartsWith(sourceProperty.Name, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var remainingName = destinationName[sourceProperty.Name.Length..];
                if (string.IsNullOrWhiteSpace(remainingName))
                {
                    continue;
                }

                if (TryResolveSourcePath(remainingName, sourceProperty.PropertyType, out var nestedPath))
                {
                    sourcePath = new[] { sourceProperty }.Concat(nestedPath).ToArray();
                    return true;
                }
            }

            sourcePath = null;
            return false;
        }

        private static bool TryResolveSourcePath(string destinationName, Type sourceType, out IReadOnlyList<PropertyInfo> sourcePath)
        {
            var sourceProperties = sourceType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.CanRead && x.GetMethod != null)
                .ToArray();

            var directProperty = sourceProperties
                .FirstOrDefault(x => string.Equals(x.Name, destinationName, StringComparison.OrdinalIgnoreCase));
            if (directProperty != null)
            {
                sourcePath = new[] { directProperty };
                return true;
            }

            return TryResolveSourcePath(destinationName, sourceProperties, out sourcePath);
        }

        private static bool IsFlattenableSourceType(Type type)
        {
            var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
            if (underlyingType == typeof(string))
            {
                return false;
            }

            if (underlyingType.IsPrimitive || underlyingType.IsEnum)
            {
                return false;
            }

            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(underlyingType))
            {
                return false;
            }

            return true;
        }

        private static bool TryGetCollectionShape(Type type, out CollectionShape shape, out Type elementType)
        {
            if (type.IsArray && type.GetArrayRank() == 1)
            {
                shape = CollectionShape.Array;
                elementType = type.GetElementType();
                return true;
            }

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
            {
                shape = CollectionShape.List;
                elementType = type.GetGenericArguments()[0];
                return true;
            }

            if (TryGetSupportedGenericCollectionElement(type, out elementType))
            {
                shape = CollectionShape.Enumerable;
                return true;
            }

            shape = CollectionShape.None;
            elementType = null;
            return false;
        }

        private static bool TryGetSupportedGenericCollectionElement(Type type, out Type elementType)
        {
            elementType = null;
            if (!type.IsGenericType)
            {
                return false;
            }

            var genericDefinition = type.GetGenericTypeDefinition();
            if (genericDefinition == typeof(IEnumerable<>)
                || genericDefinition == typeof(ICollection<>)
                || genericDefinition == typeof(IReadOnlyCollection<>)
                || genericDefinition == typeof(IList<>)
                || genericDefinition == typeof(IReadOnlyList<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }

            return false;
        }
    }
}
