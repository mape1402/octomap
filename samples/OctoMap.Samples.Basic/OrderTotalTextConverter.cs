namespace OctoMap.Samples.Basic
{
    /// <summary>
    /// Converts order totals into formatted text through dependency injection.
    /// </summary>
    public sealed class OrderTotalTextConverter : IValueConverter<decimal, string>
    {
        private readonly ICurrencyFormatter _currencyFormatter;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderTotalTextConverter"/> class.
        /// </summary>
        /// <param name="currencyFormatter">The currency formatter.</param>
        public OrderTotalTextConverter(ICurrencyFormatter currencyFormatter)
        {
            _currencyFormatter = currencyFormatter ?? throw new ArgumentNullException(nameof(currencyFormatter));
        }

        /// <inheritdoc/>
        public string Convert(decimal sourceMember, IMapContext context)
            => _currencyFormatter.Format(sourceMember, "USD");
    }
}
