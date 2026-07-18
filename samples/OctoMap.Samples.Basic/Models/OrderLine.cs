namespace OctoMap.Samples.Basic.Models
{
    /// <summary>
    /// Represents a sample order line source model.
    /// </summary>
    public sealed class OrderLine
    {
        /// <summary>
        /// Gets or sets the stock keeping unit.
        /// </summary>
        public string Sku { get; set; }

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the unit price.
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Gets or sets the ordered quantity.
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or sets the line discount.
        /// </summary>
        public decimal Discount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the line is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the line is deleted.
        /// </summary>
        public bool IsDeleted { get; set; }
    }
}
