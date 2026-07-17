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

            EnsureDestinationCanBeCreated(typeMap.DestinationType);

            var assignments = new List<MemberAssignmentPlan>();
            foreach (var destinationProperty in typeMap.DestinationType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!CanWrite(destinationProperty))
                {
                    continue;
                }

                if (explicitMemberMaps.TryGetValue(destinationProperty.Name, out var memberMap))
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
                    continue;
                }

                if (TryCreateCollectionAssignment(destinationProperty, sourceProperty, out var collectionAssignment))
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

            return new MappingPlan(typeMap.SourceType, typeMap.DestinationType, assignments);
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
                0);

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
                sourceIndex);

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
                _options.AllowNullCollections,
                false,
                null,
                false,
                null,
                0);
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
