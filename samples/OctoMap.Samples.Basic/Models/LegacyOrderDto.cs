namespace OctoMap.Samples.Basic.Models
{
    /// <summary>
    /// Represents a destination model that uses normal .NET member casing.
    /// </summary>
    public sealed class LegacyOrderDto
    {
        /// <summary>
        /// Gets or sets the customer name.
        /// </summary>
        public string CustomerName { get; set; }

        /// <summary>
        /// Gets or sets the order total.
        /// </summary>
        public decimal OrderTotal { get; set; }
    }
}
