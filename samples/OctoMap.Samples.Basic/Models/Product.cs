namespace OctoMap.Samples.Basic.Models
{
    /// <summary>
    /// Represents a sample product source model.
    /// </summary>
    public sealed class Product
    {
        /// <summary>
        /// Gets or sets the product SKU.
        /// </summary>
        public string Sku { get; set; }

        /// <summary>
        /// Gets or sets the product price.
        /// </summary>
        public decimal Price { get; set; }
    }
}
