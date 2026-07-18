namespace OctoMap.Configuration
{
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Represents explicit configuration for a destination member.
    /// </summary>
    internal sealed class MemberMap
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemberMap"/> class.
        /// </summary>
        /// <param name="destinationProperty">The destination property.</param>
        public MemberMap(PropertyInfo destinationProperty)
        {
            DestinationProperty = destinationProperty ?? throw new ArgumentNullException(nameof(destinationProperty));
        }

        /// <summary>
        /// Gets the destination property.
        /// </summary>
        public PropertyInfo DestinationProperty { get; }

        /// <summary>
        /// Gets or sets whether this member should be ignored.
        /// </summary>
        public bool IsIgnored { get; set; }

        /// <summary>
        /// Gets or sets the source expression.
        /// </summary>
        public LambdaExpression SourceExpression { get; set; }

        /// <summary>
        /// Gets or sets the resolver type.
        /// </summary>
        public Type ResolverType { get; set; }

        /// <summary>
        /// Gets or sets the converter type.
        /// </summary>
        public Type ConverterType { get; set; }

        /// <summary>
        /// Gets or sets the converter source expression.
        /// </summary>
        public LambdaExpression ConverterSourceExpression { get; set; }

        /// <summary>
        /// Gets or sets whether this member uses a constant value.
        /// </summary>
        public bool HasConstantValue { get; set; }

        /// <summary>
        /// Gets or sets the constant value.
        /// </summary>
        public object ConstantValue { get; set; }

        /// <summary>
        /// Gets or sets whether this member has a null substitute.
        /// </summary>
        public bool HasNullSubstitute { get; set; }

        /// <summary>
        /// Gets or sets the null substitute value.
        /// </summary>
        public object NullSubstitute { get; set; }

        /// <summary>
        /// Gets or sets the per-member null collection behavior override.
        /// </summary>
        public bool? AllowNullCollection { get; set; }

        /// <summary>
        /// Gets or sets the configured source index for multi-source maps.
        /// </summary>
        public int SourceIndex { get; set; }
    }
}
