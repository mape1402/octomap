namespace OctoMap.Samples.Basic
{
    /// <summary>
    /// Formats sample money values.
    /// </summary>
    public sealed class CurrencyFormatter : ICurrencyFormatter
    {
        /// <inheritdoc/>
        public string Format(decimal amount, string currencyCode)
            => $"{amount:0.00} {currencyCode}";
    }
}
