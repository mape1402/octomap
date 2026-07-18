using OctoMap.Samples.Basic.Models;

namespace OctoMap.Samples.Basic
{
    /// <summary>
    /// Runs the basic OctoMap sample scenarios.
    /// </summary>
    public sealed class SampleRunner
    {
        private readonly IOctoMapper _mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="SampleRunner"/> class.
        /// </summary>
        /// <param name="mapper">The OctoMap mapper.</param>
        public SampleRunner(IOctoMapper mapper)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }

        /// <summary>
        /// Executes the sample mappings and writes their results to the console.
        /// </summary>
        public void Run()
        {
            var customer = new Customer
            {
                Id = 100,
                FirstName = "Grace",
                LastName = "Hopper",
                InternalCode = "not-mapped"
            };

            var customerDto = _mapper.Map<Customer, CustomerDto>(customer);
            Console.WriteLine($"Configured map: {customerDto.Id} - {customerDto.FullName} - internal '{customerDto.InternalCode ?? "ignored"}'");

            var customerRecordDto = _mapper.Map<Customer, CustomerRecordDto>(customer);
            Console.WriteLine($"Constructor map: {customerRecordDto.Id} - {customerRecordDto.FullName}");

            var productDto = _mapper.Map<ProductDto>(new Product
            {
                Sku = "OCTO-001",
                Price = 49.95m
            });

            Console.WriteLine($"Implicit map: {productDto.Sku} - {productDto.Price}");

            var orderDto = _mapper.Map<Order, OrderDto>(new Order
            {
                Id = 700,
                StatusCode = "NEW",
                Description = null,
                Total = 149.99m,
                CurrencyCode = "USD",
                Customer = new Customer
                {
                    Id = 102,
                    FirstName = "Katherine",
                    LastName = "Johnson",
                    InternalCode = "nested-secret"
                },
                Items = new List<OrderItem>
                {
                    new() { Sku = "OCTO-MUG", Quantity = 2 },
                    new() { Sku = "OCTO-STICKER", Quantity = 5 }
                }
            });

            Console.WriteLine($"Resolver, value converter, nested map, collection map: {orderDto.Id} - {orderDto.Status} - {orderDto.Description} - {orderDto.StatusLabel} - {orderDto.TotalText} - {orderDto.Customer.FullName} - {orderDto.Items.Count} items");

            var orderLineDto = _mapper.Map<OrderLine, OrderLineDto>(new OrderLine
            {
                Sku = "OCTO-HOODIE",
                DisplayName = null,
                UnitPrice = 39.99m,
                Quantity = 3,
                Discount = 10m,
                IsActive = true,
                IsDeleted = false
            });

            Console.WriteLine($"Generated expressions: {orderLineDto.DisplayName} - {orderLineDto.Total} - remainder {orderLineDto.QuantityRemainder} - can ship {orderLineDto.CanShip}");

            var registeredByInterface = _mapper.Map<WarehouseItemDto>(new WarehouseItem
            {
                Code = "WH-42"
            });

            Console.WriteLine($"Interface map: {registeredByInterface.Code}");

            var summary = _mapper.Map<OrderSummaryDto>(SourceSet.Of(
                new Order
                {
                    Id = 701,
                    StatusCode = "HOLD",
                    Description = "Priority order",
                    Total = 499.50m,
                    CurrencyCode = "USD"
                },
                new Customer
                {
                    Id = 101,
                    FirstName = "Ada",
                    LastName = "Lovelace"
                }));

            Console.WriteLine($"Multi-source map: {summary.OrderId} - {summary.CustomerName} - {summary.Label}");
        }
    }
}
