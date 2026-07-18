namespace OctoMap.Samples.Basic.Models
{
    /// <summary>
    /// Represents a product SKU value object.
    /// </summary>
    public sealed class SkuCode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SkuCode"/> class.
        /// </summary>
        /// <param name="value">The SKU value.</param>
        public SkuCode(string value)
        {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        /// <summary>
        /// Gets the SKU value.
        /// </summary>
        public string Value { get; }

        /// <inheritdoc/>
        public override string ToString()
            => Value;
    }
}
