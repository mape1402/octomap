namespace OctoMap.Samples.Basic.Models
{
    /// <summary>
    /// Represents a sample destination model created from multiple sources.
    /// </summary>
    public sealed class OrderSummaryDto
    {
        /// <summary>
        /// Gets or sets the order identifier.
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Gets or sets the customer name.
        /// </summary>
        public string CustomerName { get; set; }

        /// <summary>
        /// Gets or sets the order description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the combined label.
        /// </summary>
        public string Label { get; set; }
    }
}
