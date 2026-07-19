namespace OctoMap.Samples.Basic.Models
{
    /// <summary>
    /// Represents an order source model mapped through attributes on the destination model.
    /// </summary>
    public sealed class AttributedOrder
    {
        /// <summary>
        /// Gets or sets the order status code.
        /// </summary>
        public string StatusCode { get; set; }

        /// <summary>
        /// Gets or sets the order description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets the internal order code.
        /// </summary>
        public string InternalCode { get; set; }
    }
}
