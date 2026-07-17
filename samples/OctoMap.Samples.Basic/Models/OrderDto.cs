namespace OctoMap.Samples.Basic.Models
{
    /// <summary>
    /// Represents a sample order destination model.
    /// </summary>
    public sealed class OrderDto
    {
        /// <summary>
        /// Gets or sets the order identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the order status.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the order status label.
        /// </summary>
        public string StatusLabel { get; set; }

        /// <summary>
        /// Gets or sets the formatted order total.
        /// </summary>
        public string TotalText { get; set; }

        /// <summary>
        /// Gets or sets the order description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the order customer.
        /// </summary>
        public CustomerDto Customer { get; set; }

        /// <summary>
        /// Gets or sets the order items.
        /// </summary>
        public List<OrderItemDto> Items { get; set; }
    }
}
