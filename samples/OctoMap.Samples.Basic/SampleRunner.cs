using OctoMap.Samples.Basic.Models;

namespace OctoMap.Samples.Basic
{
    /// <summary>
    /// Runs the basic OctoMap sample scenarios.
    /// </summary>
    public sealed class SampleRunner
    {
        private readonly IOctoMapper _mapper;
        private readonly IOctoMapConfiguration _configuration;
        private readonly SampleSalesDbContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="SampleRunner"/> class.
        /// </summary>
        /// <param name="mapper">The OctoMap mapper.</param>
        /// <param name="configuration">The OctoMap configuration.</param>
        /// <param name="dbContext">The Entity Framework sample database context.</param>
        public SampleRunner(
            IOctoMapper mapper,
            IOctoMapConfiguration configuration,
            SampleSalesDbContext dbContext)
        {
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
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

            var existingCustomerDto = new CustomerDto
            {
                InternalCode = "preserved"
            };
            var updatedCustomerDto = _mapper.Map(customer, existingCustomerDto);
            Console.WriteLine($"Existing destination map: same instance {ReferenceEquals(existingCustomerDto, updatedCustomerDto)} - {updatedCustomerDto.FullName} - internal '{updatedCustomerDto.InternalCode}'");

            var patchedCustomerDto = _mapper.Map(new CustomerPatch
            {
                FullName = null
            }, updatedCustomerDto);
            Console.WriteLine($"Ignore null source value patch: {patchedCustomerDto.FullName}");

            var planDescription = _configuration.DescribeMap<Customer, CustomerDto>().Split(Environment.NewLine)[0];
            Console.WriteLine($"Plan description: {planDescription}");

            var customerRecordDto = _mapper.Map<Customer, CustomerRecordDto>(customer);
            Console.WriteLine($"Constructor map: {customerRecordDto.Id} - {customerRecordDto.FullName}");

            var productDto = _mapper.Map<ProductDto>(new Product
            {
                Sku = "OCTO-001",
                Price = 49.95m
            });

            Console.WriteLine($"Implicit map: {productDto.Sku} - {productDto.Price}");

            var reversedProduct = _mapper.Map<ProductDto, Product>(productDto);
            Console.WriteLine($"Reverse map: {reversedProduct.Sku} - {reversedProduct.Price}");

            var productCodeDto = _mapper.Map<Product, ProductCodeDto>(new Product
            {
                Sku = "octo-code"
            });

            Console.WriteLine($"Global type converter map: {productCodeDto.Sku.Value}");

            var projectedProduct = new[]
            {
                new Product
                {
                    Sku = "OCTO-PROJ",
                    Price = 19.95m
                }
            }
            .AsQueryable()
            .ProjectTo<ProductDto>(_mapper.ProjectionBuilder)
            .Single();

            Console.WriteLine($"Projection map: {projectedProduct.Sku} - {projectedProduct.Price}");

            var projectedEfProduct = RunEfProjectionSample();
            Console.WriteLine($"EF SQLite projection map: {projectedEfProduct.Sku} - {projectedEfProduct.Price}");

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
                },
                Tags = new[] { "rush", "gift", "rush" }
            });

            Console.WriteLine($"Resolver, value converter, nested map, collection map, flattening: {orderDto.Id} - {orderDto.Status} - {orderDto.Description} - {orderDto.StatusLabel} - {orderDto.TotalText} - {orderDto.Customer.FullName} - {orderDto.CustomerFirstName} - {orderDto.Items.Count} items - {orderDto.Tags.Count} converted tags");

            var unflattenedOrder = _mapper.Map<OrderDto, Order>(new OrderDto
            {
                Id = 800,
                CustomerFirstName = "Dorothy"
            });

            Console.WriteLine($"ForPath map: {unflattenedOrder.Id} - {unflattenedOrder.Customer.FirstName}");

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

            var inactiveOrderLineDto = _mapper.Map<OrderLine, OrderLineDto>(new OrderLine
            {
                Sku = "OCTO-ARCHIVE",
                DisplayName = "Archived line",
                UnitPrice = 12m,
                Quantity = 1,
                IsActive = false
            });

            Console.WriteLine($"Conditional map: {inactiveOrderLineDto.DisplayName ?? "skipped"}");

            var registeredByInterface = _mapper.Map<WarehouseItemDto>(new WarehouseItem
            {
                Code = "WH-42"
            });

            Console.WriteLine($"IMapFrom interface map: {registeredByInterface.Code}");

            var registeredByMapToInterface = _mapper.Map<ShipmentEvent, ShipmentEventDto>(new ShipmentEvent
            {
                Code = "SHIP",
                Location = "Dock 7"
            });

            Console.WriteLine($"IMapTo interface map: {registeredByMapToInterface.Code} - {registeredByMapToInterface.Location}");

            var attributedOrderDto = _mapper.Map<AttributedOrder, AttributedOrderDto>(new AttributedOrder
            {
                StatusCode = "ATTR",
                Description = null,
                InternalCode = "hidden"
            });

            Console.WriteLine($"Attribute map: {attributedOrderDto.Status} - {attributedOrderDto.Description} - internal '{attributedOrderDto.InternalCode ?? "ignored"}'");

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

        private ProductDto RunEfProjectionSample()
        {
            _dbContext.Database.EnsureDeleted();
            _dbContext.Database.EnsureCreated();

            _dbContext.Products.Add(new Product
            {
                Sku = "OCTO-SQLITE",
                Price = 29.95m
            });
            _dbContext.SaveChanges();

            return _dbContext.Products
                .ProjectTo<ProductDto>(_mapper.ProjectionBuilder)
                .Single();
        }
    }
}
