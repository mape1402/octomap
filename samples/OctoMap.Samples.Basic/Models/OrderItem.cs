namespace OctoMap.Samples.Basic.Models
{
    /// <summary>
    /// Represents a sample order item source model.
    /// </summary>
    public sealed class OrderItem
    {
        /// <summary>
        /// Gets or sets the item SKU.
        /// </summary>
        public string Sku { get; set; }

        /// <summary>
        /// Gets or sets the item quantity.
        /// </summary>
        public int Quantity { get; set; }
    }
}
