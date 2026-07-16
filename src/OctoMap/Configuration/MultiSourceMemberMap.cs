namespace OctoMap.Configuration
{
    using System.Linq.Expressions;
    using System.Reflection;

    /// <summary>
    /// Represents explicit context configuration for a multi-source destination member.
    /// </summary>
    internal sealed class MultiSourceMemberMap
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MultiSourceMemberMap"/> class.
        /// </summary>
        /// <param name="destinationProperty">The destination property.</param>
        public MultiSourceMemberMap(PropertyInfo destinationProperty)
        {
            DestinationProperty = destinationProperty ?? throw new ArgumentNullException(nameof(destinationProperty));
        }

        /// <summary>
        /// Gets the destination property.
        /// </summary>
        public PropertyInfo DestinationProperty { get; }

        /// <summary>
        /// Gets or sets the source context expression.
        /// </summary>
        public LambdaExpression SourceExpression { get; set; }
    }
}
