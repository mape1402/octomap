namespace OctoMap.Samples.Basic
{
    /// <summary>
    /// Formats sample order labels.
    /// </summary>
    public sealed class OrderLabelFormatter : IOrderLabelFormatter
    {
        /// <inheritdoc/>
        public string FormatStatusLabel(int orderId, string status)
            => $"Order #{orderId:0000} is {status}";

        /// <inheritdoc/>
        public string FormatReceiptNumber(int orderId)
            => $"RCPT-{orderId:0000}";
    }
}
