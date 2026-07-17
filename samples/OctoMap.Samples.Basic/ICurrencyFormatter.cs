namespace OctoMap.Samples.Basic
{
    /// <summary>
    /// Formats money values for sample mappings.
    /// </summary>
    public interface ICurrencyFormatter
    {
        /// <summary>
        /// Formats the specified amount and currency code.
        /// </summary>
        /// <param name="amount">The monetary amount.</param>
        /// <param name="currencyCode">The currency code.</param>
        /// <returns>The formatted money value.</returns>
        string Format(decimal amount, string currencyCode);
    }
}
