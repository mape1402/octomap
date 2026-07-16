using System.Reflection;

namespace OctoMap.Planning
{
    /// <summary>
    /// Builds convention-based mapping plans.
    /// </summary>
    internal sealed class ConventionMappingPlanBuilder : IMappingPlanBuilder
    {
        /// <inheritdoc/>
        public MappingPlan Build(ITypeMap typeMap)
        {
            if (typeMap == null)
            {
                throw new ArgumentNullException(nameof(typeMap));
            }

            EnsureDestinationCanBeCreated(typeMap.DestinationType);

            var sourceProperties = typeMap.SourceType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.CanRead && x.GetMethod != null)
                .ToDictionary(x => x.Name, StringComparer.OrdinalIgnoreCase);

            var assignments = new List<MemberAssignmentPlan>();
            foreach (var destinationProperty in typeMap.DestinationType.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (!CanWrite(destinationProperty))
                {
                    continue;
                }

                if (!sourceProperties.TryGetValue(destinationProperty.Name, out var sourceProperty))
                {
                    continue;
                }

                if (!destinationProperty.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
                {
                    continue;
                }

                assignments.Add(new MemberAssignmentPlan(sourceProperty, destinationProperty));
            }

            return new MappingPlan(typeMap.SourceType, typeMap.DestinationType, assignments);
        }

        private static bool CanWrite(PropertyInfo property)
            => property.CanWrite && property.SetMethod != null && property.SetMethod.IsPublic;

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
    }
}
