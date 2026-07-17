namespace OctoMap.Samples.Basic.Models
{
    /// <summary>
    /// Represents a sample order source model.
    /// </summary>
    public sealed class Order
    {
        /// <summary>
        /// Gets or sets the order identifier.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the order status code.
        /// </summary>
        public string StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the order description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the order total amount.
        /// </summary>
        public decimal Total { get; set; }

        /// <summary>
        /// Gets or sets the order currency code.
        /// </summary>
        public string CurrencyCode { get; set; }
    }
}
