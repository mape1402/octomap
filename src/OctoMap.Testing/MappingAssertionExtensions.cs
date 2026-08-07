using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace OctoMap.Testing
{
    /// <summary>
    /// Provides assertion extensions for mapped destination objects and OctoMap testing mappers.
    /// </summary>
    public static class MappingAssertionExtensions
    {
        /// <summary>
        /// Starts assertions for a destination object mapped from the specified source object.
        /// </summary>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <param name="destination">The destination object.</param>
        /// <param name="source">The source object.</param>
        /// <returns>The mapping assertion builder.</returns>
        public static MappingAssertion<TDestination, TSource> ShouldMapFrom<TDestination, TSource>(
            this TDestination destination,
            TSource source)
            => new(destination, source);

        /// <summary>
        /// Asserts that the mapper loaded the specified profile type.
        /// </summary>
        /// <typeparam name="TProfile">The expected profile type.</typeparam>
        /// <param name="mapper">The testing mapper.</param>
        /// <returns>The same testing mapper.</returns>
        public static IOctoMapTestingMapper ShouldHaveProfile<TProfile>(this IOctoMapTestingMapper mapper)
        {
            if (mapper == null)
            {
                throw new ArgumentNullException(nameof(mapper));
            }

            var profileType = typeof(TProfile);
            var hasProfile = mapper.Configuration.GetProfiles().Any(x =>
                string.Equals(x, profileType.FullName, StringComparison.Ordinal)
                || string.Equals(x, profileType.Name, StringComparison.Ordinal));

            if (!hasProfile)
            {
                throw new OctoMapTestingAssertionException(
                    $"Expected OctoMap profile '{profileType.FullName}' to be registered. Registered profiles: {string.Join(", ", mapper.Configuration.GetProfiles())}.");
            }

            return mapper;
        }

        /// <summary>
        /// Asserts that the mapper has a configured map for the specified source and destination types.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="mapper">The testing mapper.</param>
        /// <returns>The same testing mapper.</returns>
        public static IOctoMapTestingMapper ShouldHaveMap<TSource, TDestination>(this IOctoMapTestingMapper mapper)
        {
            if (mapper == null)
            {
                throw new ArgumentNullException(nameof(mapper));
            }

            var sourceType = typeof(TSource);
            var destinationType = typeof(TDestination);
            var hasMap = mapper.Configuration.GetConfiguredMaps().Any(x =>
                x.SourceTypes.Count == 1
                && x.SourceTypes[0] == sourceType
                && x.DestinationType == destinationType);

            if (!hasMap)
            {
                throw new OctoMapTestingAssertionException(
                    $"Expected OctoMap map '{sourceType.FullName} -> {destinationType.FullName}' to be registered. Configuration: {mapper.Configuration.DescribeConfiguration()}");
            }

            return mapper;
        }

        /// <summary>
        /// Asserts that the specified query can be projected to the destination type and returns the projected query.
        /// </summary>
        /// <typeparam name="TSource">The source type.</typeparam>
        /// <typeparam name="TDestination">The destination type.</typeparam>
        /// <param name="source">The source query.</param>
        /// <param name="mapper">The testing mapper.</param>
        /// <param name="parameters">The projection parameters.</param>
        /// <returns>The projected query.</returns>
        public static IQueryable<TDestination> ShouldProjectTo<TSource, TDestination>(
            this IQueryable<TSource> source,
            IOctoMapTestingMapper mapper,
            object parameters = null)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (mapper == null)
            {
                throw new ArgumentNullException(nameof(mapper));
            }

            try
            {
                return mapper.ProjectTo<TDestination>(source, parameters);
            }
            catch (Exception exception) when (exception is not OctoMapTestingAssertionException)
            {
                throw new OctoMapTestingAssertionException(
                    $"Expected query '{typeof(TSource).FullName} -> {typeof(TDestination).FullName}' to be projectable.",
                    exception);
            }
        }

        internal static IReadOnlyList<PropertyInfo> GetPropertyPath<TDeclaring, TValue>(Expression<Func<TDeclaring, TValue>> expression)
        {
            if (expression == null)
            {
                throw new ArgumentNullException(nameof(expression));
            }

            var properties = new Stack<PropertyInfo>();
            var current = RemoveConversion(expression.Body);
            while (current is MemberExpression memberExpression)
            {
                if (memberExpression.Member is not PropertyInfo property)
                {
                    throw new ArgumentException("The expression must contain only property access.", nameof(expression));
                }

                properties.Push(property);
                current = RemoveConversion(memberExpression.Expression);
            }

            if (current != expression.Parameters[0] || properties.Count == 0)
            {
                throw new ArgumentException("The expression must be a property path that starts from the expression parameter.", nameof(expression));
            }

            return properties.ToArray();
        }

        internal static string FormatPath(IEnumerable<PropertyInfo> properties)
            => string.Join(".", properties.Select(x => x.Name));

        internal static object GetPathValue(object instance, IEnumerable<PropertyInfo> properties)
        {
            var current = instance;
            foreach (var property in properties)
            {
                if (current == null)
                {
                    return null;
                }

                current = property.GetValue(current);
            }

            return current;
        }

        internal static void AssertEquivalentValues(object expected, object actual, string path)
        {
            if (AreEquivalentValues(expected, actual))
            {
                return;
            }

            throw new OctoMapTestingAssertionException(
                $"Expected destination member '{path}' to map from source value '{FormatValue(expected)}', but found '{FormatValue(actual)}'.");
        }

        private static Expression RemoveConversion(Expression expression)
        {
            while (expression is UnaryExpression unaryExpression
                && (unaryExpression.NodeType == ExpressionType.Convert || unaryExpression.NodeType == ExpressionType.ConvertChecked))
            {
                expression = unaryExpression.Operand;
            }

            return expression;
        }

        private static bool AreEquivalentValues(object expected, object actual)
        {
            if (ReferenceEquals(expected, actual))
            {
                return true;
            }

            if (expected == null || actual == null)
            {
                return false;
            }

            if (expected is string || actual is string)
            {
                return Equals(expected, actual);
            }

            if (expected is IEnumerable expectedItems && actual is IEnumerable actualItems)
            {
                return expectedItems.Cast<object>().SequenceEqual(actualItems.Cast<object>());
            }

            return Equals(expected, actual);
        }

        private static string FormatValue(object value)
            => value == null ? "<null>" : value.ToString();
    }
}
