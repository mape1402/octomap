namespace OctoMap.Samples.Basic
{
    /// <summary>
    /// Provides display names for order status codes.
    /// </summary>
    public interface IOrderStatusCatalog
    {
        /// <summary>
        /// Gets the display name for the specified status code.
        /// </summary>
        /// <param name="statusCode">The status code.</param>
        /// <returns>The display name.</returns>
        string GetDisplayName(string statusCode);
    }
}
