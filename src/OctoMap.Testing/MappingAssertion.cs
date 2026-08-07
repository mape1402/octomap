using System.Linq.Expressions;
using System.Reflection;

namespace OctoMap.Testing
{
    /// <summary>
    /// Provides fluent assertions for a mapped destination and its source object.
    /// </summary>
    /// <typeparam name="TDestination">The destination type.</typeparam>
    /// <typeparam name="TSource">The source type.</typeparam>
    public sealed class MappingAssertion<TDestination, TSource>
    {
        private readonly TDestination _destination;
        private readonly TSource _source;

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingAssertion{TDestination, TSource}"/> class.
        /// </summary>
        /// <param name="destination">The destination object.</param>
        /// <param name="source">The source object.</param>
        public MappingAssertion(TDestination destination, TSource source)
        {
            _destination = destination;
            _source = source;
        }

        /// <summary>
        /// Asserts that the destination member maps from a source member with the same property path.
        /// </summary>
        /// <typeparam name="TValue">The member value type.</typeparam>
        /// <param name="destinationMember">The destination member expression.</param>
        /// <returns>The same assertion builder.</returns>
        public MappingAssertion<TDestination, TSource> Matching<TValue>(Expression<Func<TDestination, TValue>> destinationMember)
        {
            var destinationPath = MappingAssertionExtensions.GetPropertyPath(destinationMember);
            var sourcePath = ResolveSourcePath(destinationPath);
            return Matching(sourcePath, destinationPath);
        }

        /// <summary>
        /// Asserts that the destination member maps from the specified source member.
        /// </summary>
        /// <typeparam name="TValue">The member value type.</typeparam>
        /// <param name="sourceMember">The source member expression.</param>
        /// <param name="destinationMember">The destination member expression.</param>
        /// <returns>The same assertion builder.</returns>
        public MappingAssertion<TDestination, TSource> Matching<TValue>(
            Expression<Func<TSource, TValue>> sourceMember,
            Expression<Func<TDestination, TValue>> destinationMember)
        {
            var sourcePath = MappingAssertionExtensions.GetPropertyPath(sourceMember);
            var destinationPath = MappingAssertionExtensions.GetPropertyPath(destinationMember);
            return Matching(sourcePath, destinationPath);
        }

        private MappingAssertion<TDestination, TSource> Matching(
            IReadOnlyList<PropertyInfo> sourcePath,
            IReadOnlyList<PropertyInfo> destinationPath)
        {
            var expected = MappingAssertionExtensions.GetPathValue(_source, sourcePath);
            var actual = MappingAssertionExtensions.GetPathValue(_destination, destinationPath);
            MappingAssertionExtensions.AssertEquivalentValues(expected, actual, MappingAssertionExtensions.FormatPath(destinationPath));
            return this;
        }

        private static IReadOnlyList<PropertyInfo> ResolveSourcePath(IReadOnlyList<PropertyInfo> destinationPath)
        {
            var sourceType = typeof(TSource);
            var sourcePath = new List<PropertyInfo>(destinationPath.Count);
            foreach (var destinationProperty in destinationPath)
            {
                var sourceProperty = sourceType.GetProperty(destinationProperty.Name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (sourceProperty == null)
                {
                    throw new OctoMapTestingAssertionException(
                        $"Expected source type '{sourceType.FullName}' to expose a readable property named '{destinationProperty.Name}' for destination path '{MappingAssertionExtensions.FormatPath(destinationPath)}'.");
                }

                if (!sourceProperty.CanRead)
                {
                    throw new OctoMapTestingAssertionException(
                        $"Expected source property '{sourceType.FullName}.{sourceProperty.Name}' to be readable.");
                }

                sourcePath.Add(sourceProperty);
                sourceType = sourceProperty.PropertyType;
            }

            return sourcePath;
        }
    }
}
