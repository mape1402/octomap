using OctoMap.Samples.Basic.Models;

namespace OctoMap.Samples.Basic
{
    /// <summary>
    /// Resolves a sample order status label through dependency injection.
    /// </summary>
    public sealed class OrderStatusLabelResolver : IValueResolver<Order, OrderDto, string>
    {
        private readonly IOrderStatusCatalog _statusCatalog;
        private readonly IOrderLabelFormatter _labelFormatter;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderStatusLabelResolver"/> class.
        /// </summary>
        /// <param name="statusCatalog">The order status catalog.</param>
        /// <param name="labelFormatter">The order label formatter.</param>
        public OrderStatusLabelResolver(IOrderStatusCatalog statusCatalog, IOrderLabelFormatter labelFormatter)
        {
            _statusCatalog = statusCatalog ?? throw new ArgumentNullException(nameof(statusCatalog));
            _labelFormatter = labelFormatter ?? throw new ArgumentNullException(nameof(labelFormatter));
        }

        /// <inheritdoc/>
        public string Resolve(Order source, OrderDto destination, IMapContext context)
        {
            var status = _statusCatalog.GetDisplayName(source.StatusCode);
            return _labelFormatter.FormatStatusLabel(source.Id, status);
        }
    }
}
