namespace OctoMap.Samples.Basic.Models
{
    /// <summary>
    /// Represents a sample order line destination model.
    /// </summary>
    public sealed class OrderLineDto
    {
        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the calculated line total.
        /// </summary>
        public decimal Total { get; set; }

        /// <summary>
        /// Gets or sets the quantity remainder.
        /// </summary>
        public int QuantityRemainder { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the line can ship.
        /// </summary>
        public bool CanShip { get; set; }
    }
}
