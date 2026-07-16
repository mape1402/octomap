namespace OctoMap.Samples.Basic.Models
{
    /// <summary>
    /// Represents a sample product destination model.
    /// </summary>
    public sealed class ProductDto
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
