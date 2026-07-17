namespace OctoMap.Samples.Basic
{
    /// <summary>
    /// Formats order labels used by mapping resolvers and converters.
    /// </summary>
    public interface IOrderLabelFormatter
    {
        /// <summary>
        /// Formats a status label for the specified order.
        /// </summary>
        /// <param name="orderId">The order identifier.</param>
        /// <param name="status">The display status.</param>
        /// <returns>The formatted status label.</returns>
        string FormatStatusLabel(int orderId, string status);

        /// <summary>
        /// Formats a receipt number for the specified order.
        /// </summary>
        /// <param name="orderId">The order identifier.</param>
        /// <returns>The formatted receipt number.</returns>
        string FormatReceiptNumber(int orderId);
    }
}
