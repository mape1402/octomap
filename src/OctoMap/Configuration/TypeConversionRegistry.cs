using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

namespace OctoMap.Configuration
{
    /// <summary>
    /// Provides configured and built-in type conversions.
    /// </summary>
    internal sealed class TypeConversionRegistry : ITypeConversionRegistry
    {
        private static readonly HashSet<Type> NumericTypes = new()
        {
            typeof(byte),
            typeof(sbyte),
            typeof(short),
            typeof(ushort),
            typeof(int),
            typeof(uint),
            typeof(long),
            typeof(ulong),
            typeof(float),
            typeof(double),
            typeof(decimal)
        };

        private readonly Dictionary<MapKey, TypeConversionMap> _conversions;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeConversionRegistry"/> class.
        /// </summary>
        /// <param name="conversions">The configured conversions.</param>
        public TypeConversionRegistry(IEnumerable<TypeConversionMap> conversions)
        {
            _conversions = (conversions ?? Array.Empty<TypeConversionMap>())
                .ToDictionary(x => new MapKey(x.SourceType, x.DestinationType));
        }

        /// <inheritdoc/>
        public IReadOnlyCollection<TypeConversionMap> Conversions => _conversions.Values.ToArray();

        /// <inheritdoc/>
        public bool TryFind(Type sourceType, Type destinationType, out TypeConversionMap conversion)
        {
            if (sourceType == null)
            {
                throw new ArgumentNullException(nameof(sourceType));
            }

            if (destinationType == null)
            {
                throw new ArgumentNullException(nameof(destinationType));
            }

            if (_conversions.TryGetValue(new MapKey(sourceType, destinationType), out conversion))
            {
                return true;
            }

            return TryCreateBuiltIn(sourceType, destinationType, out conversion);
        }

        private static bool TryCreateBuiltIn(Type sourceType, Type destinationType, out TypeConversionMap conversion)
        {
            if (destinationType.IsAssignableFrom(sourceType))
            {
                conversion = null;
                return false;
            }

            var sourceUnderlying = Nullable.GetUnderlyingType(sourceType);
            var destinationUnderlying = Nullable.GetUnderlyingType(destinationType);
            var nonNullableSource = sourceUnderlying ?? sourceType;
            var nonNullableDestination = destinationUnderlying ?? destinationType;

            if (sourceUnderlying != null && nonNullableDestination.IsAssignableFrom(nonNullableSource))
            {
                conversion = CreateExpressionConversion(sourceType, destinationType, value => Expression.Convert(value, destinationType));
                return true;
            }

            if (destinationUnderlying != null && nonNullableDestination.IsAssignableFrom(sourceType))
            {
                conversion = CreateExpressionConversion(sourceType, destinationType, value => Expression.Convert(value, destinationType));
                return true;
            }

            if (nonNullableSource.IsEnum && nonNullableDestination == typeof(string))
            {
                conversion = CreateExpressionConversion(sourceType, destinationType, value => Expression.Call(value, nameof(ToString), Type.EmptyTypes));
                return true;
            }

            if (nonNullableSource == typeof(string) && nonNullableDestination.IsEnum)
            {
                conversion = CreateExpressionConversion(sourceType, destinationType, value =>
                {
                    var parse = typeof(Enum)
                        .GetMethods(BindingFlags.Public | BindingFlags.Static)
                        .Single(x => x.Name == nameof(Enum.Parse)
                            && x.IsGenericMethodDefinition
                            && x.GetParameters().Length == 2
                            && x.GetParameters()[0].ParameterType == typeof(string)
                            && x.GetParameters()[1].ParameterType == typeof(bool))
                        .MakeGenericMethod(nonNullableDestination);
                    var parsed = Expression.Call(parse, value, Expression.Constant(true));
                    return Expression.Convert(parsed, destinationType);
                });
                return true;
            }

            if (nonNullableSource == typeof(string) && TryGetParseMethod(nonNullableDestination, out var parseMethod))
            {
                conversion = CreateExpressionConversion(sourceType, destinationType, value =>
                {
                    var parsed = BuildParseCall(value, nonNullableDestination, parseMethod);
                    return parsed.Type == destinationType ? parsed : Expression.Convert(parsed, destinationType);
                });
                return true;
            }

            if (nonNullableDestination == typeof(string) && IsPrimitiveStringConvertible(nonNullableSource))
            {
                conversion = CreateExpressionConversion(sourceType, destinationType, value => Expression.Call(value, nameof(ToString), Type.EmptyTypes));
                return true;
            }

            if (IsNumeric(nonNullableSource) && IsNumeric(nonNullableDestination))
            {
                conversion = CreateExpressionConversion(sourceType, destinationType, value => Expression.Convert(value, destinationType));
                return true;
            }

            conversion = null;
            return false;
        }

        private static TypeConversionMap CreateExpressionConversion(
            Type sourceType,
            Type destinationType,
            Func<Expression, Expression> buildBody)
        {
            var source = Expression.Parameter(sourceType, "source");
            var body = buildBody(source);
            return new TypeConversionMap(sourceType, destinationType, Expression.Lambda(body, source), null);
        }

        private static bool TryGetParseMethod(Type type, out MethodInfo parseMethod)
        {
            parseMethod = type.GetMethod(
                "Parse",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(string), typeof(IFormatProvider) },
                modifiers: null);
            if (parseMethod != null)
            {
                return true;
            }

            parseMethod = type.GetMethod(
                "Parse",
                BindingFlags.Public | BindingFlags.Static,
                binder: null,
                types: new[] { typeof(string) },
                modifiers: null);
            return parseMethod != null;
        }

        private static Expression BuildParseCall(Expression value, Type destinationType, MethodInfo parseMethod)
        {
            if (parseMethod.GetParameters().Length == 2)
            {
                return Expression.Call(parseMethod, value, Expression.Property(null, typeof(CultureInfo), nameof(CultureInfo.InvariantCulture)));
            }

            return Expression.Call(parseMethod, value);
        }

        private static bool IsPrimitiveStringConvertible(Type type)
            => type == typeof(Guid)
                || type == typeof(DateTime)
                || type == typeof(DateTimeOffset)
                || type == typeof(TimeSpan)
                || type == typeof(bool)
                || IsNumeric(type);

        private static bool IsNumeric(Type type)
            => NumericTypes.Contains(type);
    }
}
