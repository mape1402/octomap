using Microsoft.Extensions.DependencyInjection;
using OctoMap;

var services = new ServiceCollection();

services.AddOctoMap(
    options => options.EnableRuntimeImplicitMaps = true,
    typeof(SalesProfile).Assembly);

var provider = services.BuildServiceProvider();
var mapper = provider.GetRequiredService<IOctoMapper>();

var customer = new Customer
{
    Id = 100,
    FirstName = "Grace",
    LastName = "Hopper",
    InternalCode = "not-mapped"
};

var customerDto = mapper.Map<Customer, CustomerDto>(customer);
Console.WriteLine($"Configured map: {customerDto.Id} - {customerDto.FullName} - internal '{customerDto.InternalCode ?? "ignored"}'");

var productDto = mapper.Map<ProductDto>(new Product
{
    Sku = "OCTO-001",
    Price = 49.95m
});

Console.WriteLine($"Implicit map: {productDto.Sku} - {productDto.Price}");

var orderDto = mapper.Map<Order, OrderDto>(new Order
{
    Id = 700,
    Description = null
});

Console.WriteLine($"Value rules: {orderDto.Id} - {orderDto.Status} - {orderDto.Description}");

var registeredByInterface = mapper.Map<WarehouseItemDto>(new WarehouseItem
{
    Code = "WH-42"
});

Console.WriteLine($"Interface map: {registeredByInterface.Code}");

/// <summary>
/// Defines sample maps for the sales domain.
/// </summary>
public sealed class SalesProfile : OctoMapProfile
{
    /// <inheritdoc/>
    public override void Configure(IOctoMapConfigurationBuilder builder)
    {
        builder.CreateMap<Customer, CustomerDto>()
            .ForMember(x => x.FullName, x => x.MapFrom(s => s.FirstName + " " + s.LastName))
            .ForMember(x => x.InternalCode, x => x.Ignore());

        builder.CreateMap<Order, OrderDto>()
            .ForMember(x => x.Status, x => x.UseValue("Created"))
            .ForMember(x => x.Description, x => x.NullSubstitute("No description"));
    }
}

/// <summary>
/// Represents a sample customer source model.
/// </summary>
public sealed class Customer
{
    /// <summary>
    /// Gets or sets the customer identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the customer first name.
    /// </summary>
    public string FirstName { get; set; }

    /// <summary>
    /// Gets or sets the customer last name.
    /// </summary>
    public string LastName { get; set; }

    /// <summary>
    /// Gets or sets an unmapped internal code.
    /// </summary>
    public string InternalCode { get; set; }
}

/// <summary>
/// Represents a sample customer destination model.
/// </summary>
public sealed class CustomerDto
{
    /// <summary>
    /// Gets or sets the customer identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the customer full name.
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    /// Gets or sets the internal code.
    /// </summary>
    public string InternalCode { get; set; }
}

/// <summary>
/// Represents a sample product source model.
/// </summary>
public sealed class Product
{
    /// <summary>
    /// Gets or sets the product SKU.
    /// </summary>
    public string Sku { get; set; }

    /// <summary>
    /// Gets or sets the product price.
    /// </summary>
    public decimal Price { get; set; }
}

/// <summary>
/// Represents a sample product destination model.
/// </summary>
public sealed class ProductDto
{
    /// <summary>
    /// Gets or sets the product SKU.
    /// </summary>
    public string Sku { get; set; }

    /// <summary>
    /// Gets or sets the product price.
    /// </summary>
    public decimal Price { get; set; }
}

/// <summary>
/// Represents a sample order source model.
/// </summary>
public sealed class Order
{
    /// <summary>
    /// Gets or sets the order identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the order description.
    /// </summary>
    public string Description { get; set; }
}

/// <summary>
/// Represents a sample order destination model.
/// </summary>
public sealed class OrderDto
{
    /// <summary>
    /// Gets or sets the order identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the order status.
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Gets or sets the order description.
    /// </summary>
    public string Description { get; set; }
}

/// <summary>
/// Represents a warehouse item source model.
/// </summary>
public sealed class WarehouseItem
{
    /// <summary>
    /// Gets or sets the warehouse item code.
    /// </summary>
    public string Code { get; set; }
}

/// <summary>
/// Represents a warehouse item destination model declared through IMapFrom.
/// </summary>
public sealed class WarehouseItemDto : IMapFrom<WarehouseItem>
{
    /// <summary>
    /// Gets or sets the warehouse item code.
    /// </summary>
    public string Code { get; set; }
}
