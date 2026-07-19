namespace OctoMap.Samples.Basic.Models
{
    /// <summary>
    /// Represents a legacy source model that uses snake_case member names.
    /// </summary>
    public sealed class LegacyOrder
    {
        /// <summary>
        /// Gets or sets the legacy customer name.
        /// </summary>
        public string customer_name { get; set; }

        /// <summary>
        /// Gets or sets the legacy order total.
        /// </summary>
        public decimal order_total { get; set; }
    }
}
