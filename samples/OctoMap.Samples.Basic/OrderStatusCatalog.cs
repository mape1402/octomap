namespace OctoMap.Samples.Basic
{
    /// <summary>
    /// Provides sample order status names.
    /// </summary>
    public sealed class OrderStatusCatalog : IOrderStatusCatalog
    {
        /// <inheritdoc/>
        public string GetDisplayName(string statusCode)
            => statusCode switch
            {
                "NEW" => "Created",
                "PAID" => "Paid",
                "HOLD" => "On hold",
                _ => "Unknown"
            };
    }
}
