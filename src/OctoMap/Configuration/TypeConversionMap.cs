using System.Linq.Expressions;

namespace OctoMap.Configuration
{
    /// <summary>
    /// Represents a configured global type conversion.
    /// </summary>
    public sealed class TypeConversionMap
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TypeConversionMap"/> class.
        /// </summary>
        /// <param name="sourceType">The source type.</param>
        /// <param name="destinationType">The destination type.</param>
        /// <param name="conversionExpression">The projectable conversion expression.</param>
        /// <param name="converterType">The DI converter type.</param>
        public TypeConversionMap(
            Type sourceType,
            Type destinationType,
            LambdaExpression conversionExpression,
            Type converterType)
        {
            SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
            DestinationType = destinationType ?? throw new ArgumentNullException(nameof(destinationType));
            ConversionExpression = conversionExpression;
            ConverterType = converterType;
        }

        /// <summary>
        /// Gets the source type.
        /// </summary>
        public Type SourceType { get; }

        /// <summary>
        /// Gets the destination type.
        /// </summary>
        public Type DestinationType { get; }

        /// <summary>
        /// Gets the projectable conversion expression.
        /// </summary>
        public LambdaExpression ConversionExpression { get; }

        /// <summary>
        /// Gets the DI converter type.
        /// </summary>
        public Type ConverterType { get; }

        /// <summary>
        /// Gets whether this conversion uses a DI converter.
        /// </summary>
        public bool UsesServiceConverter => ConverterType != null;
    }
}
