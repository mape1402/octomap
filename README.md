# OctoMap

**OctoMap** is a lightweight .NET object mapping library built on top of **DynaBee** runtime code generation.

OctoMap is designed for applications that want AutoMapper-style configuration, but prefer generated mapper types over reflection-heavy runtime mapping. Profiles describe the mapping rules, OctoMap turns those rules into mapping plans, and DynaBee generates the executable mapper classes and method invokers.

## What OctoMap Does

- Configures maps through profiles.
- Supports explicit member rules with a fluent API.
- Supports convention-based single-source mapping by matching property names.
- Supports runtime implicit single-source maps with caching.
- Supports interface-based registration through `IMapFrom<T>` and `IMapTo<T>`.
- Supports explicit multi-source maps into one destination.
- Supports nested object mapping.
- Supports array, `List<T>`, and common collection interface member mapping.
- Supports DI-based value resolvers and value converters.
- Uses DynaBee-generated method bodies and invokers for hot execution paths.
- Integrates with `Microsoft.Extensions.DependencyInjection`.

## Design Goals

OctoMap is intentionally split from the generation engine.

OctoMap owns:

- profile discovery
- mapping configuration
- mapping plan creation
- validation
- runtime map cache
- public mapper APIs

DynaBee owns:

- generated type creation
- generated method body creation
- generated instance creation
- generated method invocation
- low-level runtime code generation details

This boundary keeps OctoMap focused on mapping behavior while allowing DynaBee to evolve as a general-purpose runtime generation engine.

## Requirements

- .NET SDK 10.0+ recommended for development.
- The library multi-targets `net8.0`, `net9.0`, and `net10.0`.

## Installation

OctoMap is currently in alpha. Once packaged, installation will use the normal NuGet flow:

```bash
dotnet add package OctoMap --prerelease
```

For local development, reference the project directly or use the solution in this repository.

## Quick Start

Register OctoMap through dependency injection and pass the assemblies that contain profiles or interface-based map declarations.

```csharp
using Microsoft.Extensions.DependencyInjection;
using OctoMap;

var services = new ServiceCollection();

services.AddOctoMap(typeof(SalesProfile).Assembly);

var provider = services.BuildServiceProvider();
var mapper = provider.GetRequiredService<IOctoMapper>();
```

Define a profile:

```csharp
public sealed class SalesProfile : OctoMapProfile
{
    public override void Configure(IOctoMapConfigurationBuilder builder)
    {
        builder.CreateMap<Customer, CustomerDto>()
            .ForMember(x => x.FullName, x => x.MapFrom(s => s.FirstName + " " + s.LastName))
            .ForMember(x => x.InternalCode, x => x.Ignore());
    }
}
```

Map objects:

```csharp
var dto = mapper.Map<Customer, CustomerDto>(customer);
```

## Single-Source Maps

Single-source maps are configured with `CreateMap<TSource, TDestination>()`.

```csharp
builder.CreateMap<Customer, CustomerDto>();
```

By default, OctoMap maps matching public readable source properties to public writable destination properties.

```csharp
public sealed class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public sealed class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; }
}
```

## Member Rules

Use `ForMember(...)` to customize destination members.

```csharp
builder.CreateMap<Customer, CustomerDto>()
    .ForMember(x => x.FullName, x => x.MapFrom(s => s.FirstName + " " + s.LastName))
    .ForMember(x => x.Status, x => x.UseValue("Active"))
    .ForMember(x => x.Description, x => x.NullSubstitute("No description"))
    .ForMember(x => x.InternalCode, x => x.Ignore());
```

Supported member rules:

- `MapFrom(...)`: maps from a source expression.
- `ConvertUsing<TConverter>(...)`: converts a source member value through a DI service.
- `ResolveUsing<TResolver>()`: resolves a member through a DI service.
- `UseValue(...)`: assigns a constant value.
- `NullSubstitute(...)`: replaces null source results for reference-type destination members.
- `PreCondition(...)`: skips value resolution and assignment when the source predicate fails.
- `Condition(...)`: skips assignment when the source or resolved value predicate fails.
- `Ignore()`: excludes a destination member.

`MapFrom(...)` supports common generated expression shapes, including member access, constants, conversions, conditional expressions, arithmetic, comparisons, short-circuiting boolean logic, boolean negation, null coalescing, string concatenation, instance method calls, and static method calls.

```csharp
builder.CreateMap<OrderLine, OrderLineDto>()
    .ForMember(x => x.Total, x => x.MapFrom(s => (s.UnitPrice * s.Quantity) - s.Discount))
    .ForMember(x => x.CanShip, x => x.MapFrom(s => s.IsActive && s.Quantity > 0 && !s.IsDeleted))
    .ForMember(x => x.DisplayName, x => x.MapFrom(s => (s.DisplayName ?? s.Sku ?? "Unknown").Trim().ToUpperInvariant()))
    .ForMember(x => x.RoundedTotal, x => x.MapFrom(s => decimal.Round(s.Total, 2)));
```

## Conditional Mapping

Use `PreCondition(...)` when the member should not even resolve its value unless the source predicate passes.

```csharp
builder.CreateMap<OrderLine, OrderLineDto>()
    .ForMember(x => x.DisplayName, x =>
    {
        x.MapFrom(s => s.DisplayName.Trim().ToUpperInvariant());
        x.PreCondition(s => s.IsActive);
    });
```

Use `Condition(...)` when the value should be resolved first, then conditionally assigned.

```csharp
builder.CreateMap<OrderLine, OrderLineDto>()
    .ForMember(x => x.DisplayName, x =>
    {
        x.MapFrom(s => s.DisplayName);
        x.Condition((source, value) => value != "skip");
    });
```

`PreCondition(...)` and `Condition(...)` are supported by runtime mapping for direct members, resolvers, converters, nested maps, collection maps, `ForPath(...)`, and source contributions in multi-source maps. Projection support is planned separately.

## DI-Based Value Converters

Value converters transform one source member value into one destination member value. They are useful when the conversion depends on application services, formatting rules, localization, or domain policies.

```csharp
public sealed class OrderTotalTextConverter : IValueConverter<decimal, string>
{
    private readonly ICurrencyFormatter _currencyFormatter;

    public OrderTotalTextConverter(ICurrencyFormatter currencyFormatter)
    {
        _currencyFormatter = currencyFormatter;
    }

    public string Convert(decimal sourceMember, IMapContext context)
        => _currencyFormatter.Format(sourceMember, "USD");
}
```

Configure the member with `ConvertUsing<TConverter>(...)`.

```csharp
builder.CreateMap<Order, OrderDto>()
    .ForMember(x => x.TotalText, x => x.ConvertUsing<OrderTotalTextConverter>(s => s.Total));
```

Register the converter and its dependencies in DI.

```csharp
services.AddSingleton<ICurrencyFormatter, CurrencyFormatter>();
services.AddTransient<OrderTotalTextConverter>();
services.AddOctoMap(typeof(SalesProfile).Assembly);
```

Converters are resolved from `IMapContext.Services` on every `Map` call, so scoped, transient, and singleton lifetimes remain controlled by the application service provider.

## DI-Based Resolvers

Resolvers let a destination member use the full source object, the partially built destination object, and application services while still keeping mapping configuration declarative.

```csharp
public sealed class OrderStatusLabelResolver
    : IValueResolver<Order, OrderDto, string>
{
    private readonly IOrderStatusCatalog _statusCatalog;
    private readonly IOrderLabelFormatter _labelFormatter;

    public OrderStatusLabelResolver(
        IOrderStatusCatalog statusCatalog,
        IOrderLabelFormatter labelFormatter)
    {
        _statusCatalog = statusCatalog;
        _labelFormatter = labelFormatter;
    }

    public string Resolve(Order source, OrderDto destination, IMapContext context)
    {
        var status = _statusCatalog.GetDisplayName(source.StatusCode);
        return _labelFormatter.FormatStatusLabel(source.Id, status);
    }
}
```

Configure the member with `ResolveUsing<TResolver>()`.

```csharp
builder.CreateMap<Order, OrderDto>()
    .ForMember(x => x.StatusLabel, x => x.ResolveUsing<OrderStatusLabelResolver>());
```

Register the resolver in DI.

```csharp
services.AddTransient<OrderStatusLabelResolver>();
services.AddOctoMap(typeof(SalesProfile).Assembly);
```

Resolvers are resolved from `IMapContext.Services` on every `Map` call. OctoMap does not cache resolver instances, so service lifetimes remain controlled by the application's service provider.

## Runtime Implicit Maps

OctoMap can create and cache single-source convention maps at runtime when enabled.

```csharp
services.AddOctoMap(
    options => options.EnableRuntimeImplicitMaps = true,
    typeof(SalesProfile).Assembly);

var dto = mapper.Map<ProductDto>(product);
```

Runtime implicit maps are useful when source and destination shapes already match.

```csharp
public sealed class Product
{
    public string Sku { get; set; }
    public decimal Price { get; set; }
}

public sealed class ProductDto
{
    public string Sku { get; set; }
    public decimal Price { get; set; }
}
```

Runtime implicit maps are single-source only. Multi-source maps must be configured explicitly.

## Reverse Mapping

Single-source maps can register a basic reverse map.

```csharp
builder.CreateMap<Product, ProductDto>()
    .ReverseMap();
```

The reverse map supports convention member matching and direct reversible `MapFrom(...)` rules.

```csharp
builder.CreateMap<Customer, CustomerDto>()
    .ForMember(x => x.DisplayName, x => x.MapFrom(s => s.Name))
    .ReverseMap();
```

This can map `CustomerDto.DisplayName` back to `Customer.Name`. Reverse mapping does not currently reverse resolvers, converters, flattening, unflattening, complex expressions, or multi-source maps.

## Projection Mapping

OctoMap can build LINQ projection expressions for query providers such as Entity Framework.

```csharp
var mapper = provider.GetRequiredService<IOctoMapper>();

IQueryable<ProductDto> query = db.Products
    .ProjectTo<Product, ProductDto>(mapper);
```

`ProjectTo(...)` uses OctoMap configuration through `IOctoMapper` and builds an `Expression<Func<TSource, TDestination>>` behind the scenes. Projection is intentionally separate from the DynaBee runtime backend because LINQ providers need expression trees they can translate.

You can also call projection directly from the mapper facade:

```csharp
IQueryable<ProductDto> query = mapper.ProjectTo<Product, ProductDto>(db.Products);
```

The first projection pass supports:

- direct property mapping
- configured `MapFrom(...)` expressions
- constants
- null substitutes
- convention flattening
- constructor projection for records and immutable DTOs

Runtime-only features are rejected with clear errors:

- DI resolvers
- DI value converters
- conditional mapping
- nested runtime mapping
- collection runtime mapping
- multi-source maps

## Constructor Mapping

OctoMap can create destinations through constructors. If a destination does not expose a public parameterless constructor, OctoMap tries to match public constructor parameters to readable source properties by name.

```csharp
public sealed record CustomerDto(int Id, string Name);

builder.CreateMap<Customer, CustomerDto>();
```

You can also configure construction explicitly.

```csharp
builder.CreateMap<Customer, CustomerDto>()
    .ConstructUsing(s => new CustomerDto(s.Id, s.FirstName.Trim() + " " + s.LastName.Trim()));
```

After construction, OctoMap still applies configured and convention member assignments for public settable destination properties that were not already supplied through the constructor.

## Nested Object Mapping

OctoMap can map nested object members by convention when the source and destination property names match and the member types are mappable.

```csharp
public sealed class Order
{
    public Customer Customer { get; set; }
}

public sealed class OrderDto
{
    public CustomerDto Customer { get; set; }
}
```

Configure the parent and child maps:

```csharp
builder.CreateMap<Order, OrderDto>();

builder.CreateMap<Customer, CustomerDto>()
    .ForMember(x => x.FullName, x => x.MapFrom(s => s.FirstName + " " + s.LastName));
```

The generated `Order -> OrderDto` mapper calls the cached `Customer -> CustomerDto` map for the nested member. If the nested source value is `null`, the destination member is assigned `null`.

If a nested child map is not configured and runtime implicit maps are enabled, OctoMap can create the child map by convention. If runtime implicit maps are disabled, the map fails with a clear runtime configuration error.

## Flattening

Single-source maps support flattening by convention. If a destination member name can be split into a readable source property path, OctoMap maps that path directly.

```csharp
public sealed class Order
{
    public Customer Customer { get; set; }
}

public sealed class Customer
{
    public string Name { get; set; }
}

public sealed class OrderDto
{
    public string CustomerName { get; set; }
}

builder.CreateMap<Order, OrderDto>();
```

This maps `Order.Customer.Name` to `OrderDto.CustomerName`. If an intermediate source object is null, OctoMap assigns the destination member default value. Flattening is currently single-source only; multi-source maps must configure those members explicitly.

## Destination Paths

Use `ForPath(...)` when a flat source model needs to assign a nested destination member explicitly.

```csharp
builder.CreateMap<OrderDto, Order>()
    .ForPath(x => x.Customer.Name, x => x.MapFrom(s => s.CustomerName));
```

OctoMap creates null intermediate destination objects when their types expose public parameterless constructors. If an intermediate path member is abstract, an interface, a value type, readonly, or missing a public parameterless constructor, validation fails with a clear configuration error.

`ForPath(...)` is currently runtime-only. Projection support for nested destination paths is planned separately because query providers need nested `MemberInit` expressions that must stay provider-friendly.

## Collection Mapping

OctoMap supports collection member mapping by convention when the source and destination property names match and the element types are assignable or mappable.

Supported source shapes:

- `T[]`
- `List<T>`
- `IEnumerable<T>`
- `ICollection<T>`
- `IReadOnlyCollection<T>`
- `IList<T>`
- `IReadOnlyList<T>`

Supported destination shapes:

- `T[]`
- `List<T>`
- `IEnumerable<T>`
- `ICollection<T>`
- `IReadOnlyCollection<T>`
- `IList<T>`
- `IReadOnlyList<T>`

Example:

```csharp
public sealed class Order
{
    public IEnumerable<OrderItem> Items { get; set; }
}

public sealed class OrderDto
{
    public IReadOnlyList<OrderItemDto> Items { get; set; }
}
```

Configure the parent and item maps:

```csharp
builder.CreateMap<Order, OrderDto>();

builder.CreateMap<OrderItem, OrderItemDto>()
    .ForMember(x => x.Label, x => x.MapFrom(s => s.Sku + " x " + s.Quantity));
```

OctoMap generates a loop through DynaBee. If the item type needs a map, OctoMap uses the cached item mapper per element. If the item type is already assignable, OctoMap copies the item value/reference.

By default, null source collections map to null destination collections. You can map null collections to empty collections globally:

```csharp
services.AddOctoMap(
    options => options.AllowNullCollections = false,
    typeof(SalesProfile).Assembly);
```

You can also override the behavior for a specific collection member:

```csharp
builder.CreateMap<Order, OrderDto>()
    .ForMember(x => x.Items, x => x.UseEmptyCollectionWhenNull());
```

Or explicitly preserve null for one member when the global option maps null collections to empty collections:

```csharp
builder.CreateMap<Order, OrderDto>()
    .ForMember(x => x.Items, x => x.AllowNullCollection(true));
```

## Interface-Based Registration

Types can opt into map registration with marker interfaces.

```csharp
public sealed class WarehouseItemDto : IMapFrom<WarehouseItem>
{
    public string Code { get; set; }
}
```

When the containing assembly is passed to `AddOctoMap(...)`, OctoMap registers:

```csharp
WarehouseItem -> WarehouseItemDto
```

`IMapTo<TDestination>` registers the opposite direction:

```csharp
public sealed class Customer : IMapTo<CustomerDto>
{
    public string Name { get; set; }
}
```

## Explicit Multi-Source Maps

Multi-source maps create one destination object from multiple source objects.

They are intentionally explicit:

- no runtime implicit creation
- no convention matching
- no automatic conflict resolution

Configure each source contribution with `.From<TSource>(...)`.

```csharp
builder.CreateMultiMap<OrderSummaryDto>()
    .From<Order>(map => map
        .ForMember(x => x.OrderId, x => x.MapFrom(s => s.Id))
        .ForMember(x => x.Description, x => x.MapFrom(s => s.Description)))
    .From<Customer>(map => map
        .ForMember(x => x.CustomerName, x => x.MapFrom(s => s.FirstName)));
```

When a destination member needs more than one source, use the multi-source context:

```csharp
builder.CreateMultiMap<OrderSummaryDto>()
    .From<Order>(map => map
        .ForMember(x => x.OrderId, x => x.MapFrom(s => s.Id)))
    .From<Customer>(map => map
        .ForMember(x => x.CustomerName, x => x.MapFrom(s => s.FirstName)))
    .ForMember(x => x.Label, x => x.MapFrom(ctx =>
        ctx.Get<Order>().Description + " - " + ctx.Get<Customer>().FirstName));
```

Map at runtime with `SourceSet`:

```csharp
var dto = mapper.Map<OrderSummaryDto>(SourceSet.Of(order, customer));
```

Multi-source rules:

- A multi-source map must be registered with `CreateMultiMap<TDestination>()`.
- Destination members must be mapped explicitly.
- Convention matching is disabled.
- Runtime implicit mapping is disabled.
- The same destination member cannot be configured more than once.
- `ctx.Get<TSource>()` must resolve to exactly one configured source assignable to `TSource`.
- `SourceSet.Get<TSource>()` throws if no source matches or more than one source matches.

## Validation

OctoMap validates configured maps before compiling generated mappers. Invalid configuration throws `OctoMapValidationException` during runtime compilation or when explicitly asserted.

```csharp
var configuration = provider.GetRequiredService<IOctoMapConfiguration>();

configuration.AssertValid();
```

Validation catches cases such as:

- destination types without a public parameterless constructor
- destination members without public setters
- unsupported expression nodes
- invalid constant values
- invalid null substitutes
- duplicate multi-source destination member configuration
- ambiguous `ctx.Get<TSource>()` calls

## Plan Diagnostics

OctoMap can expose the mapping plan used by runtime generation and projections.

```csharp
var plan = configuration.GetPlan<Order, OrderDto>();
var description = configuration.DescribeMap<Order, OrderDto>();
```

Example description:

```text
OrderDto.Id <- Order.Id
OrderDto.CustomerName <- Order.Customer.Name
OrderDto.TotalText <- OrderTotalTextConverter(Order.Total)
OrderDto.StatusLabel <- OrderStatusLabelResolver
```

This diagnostic path is built from OctoMap planning data. It does not expose DynaBee contracts or generated runtime types.

## Runtime Behavior

OctoMap caches compiled maps by source type set and destination type.

For a configured single-source map:

```text
Customer -> CustomerDto
```

OctoMap builds one generated mapper and reuses it.

For a multi-source map:

```text
Order, Customer -> OrderSummaryDto
```

OctoMap builds one generated mapper whose generated method receives all source parameters and the map context.

DynaBee is used for:

- mapper class generation
- mapper method body generation
- mapper instance creation
- generated method invocation

## Sample Project

Run the basic sample:

```bash
dotnet run --framework net8.0 --project samples/OctoMap.Samples.Basic/OctoMap.Samples.Basic.csproj
```

Expected output:

```text
Configured map: 100 - Grace Hopper - internal 'ignored'
Plan description: CustomerDto.Id <- Customer.Id
Constructor map: 100 - Grace Hopper
Implicit map: OCTO-001 - 49.95
Reverse map: OCTO-001 - 49.95
Projection map: OCTO-PROJ - 19.95
EF SQLite projection map: OCTO-SQLITE - 29.95
Resolver, value converter, nested map, collection map, flattening: 700 - NEW - No description - Order #0700 is Created - 149.99 USD - Katherine Johnson - Katherine - 2 items
ForPath map: 800 - Dorothy
Generated expressions: OCTO-HOODIE - 109.97 - remainder 1 - can ship True
Conditional map: skipped
Interface map: WH-42
Multi-source map: 701 - Ada - Priority order - Ada
```

## Development

```bash
dotnet restore
dotnet build
dotnet test
```

## Benchmarks

```bash
dotnet run -c Release -f net8.0 --project benchmarks/OctoMap.Benchmarks/OctoMap.Benchmarks.csproj -- --filter *
```

## Current Status

OctoMap is in early alpha. The core runtime path, explicit maps, runtime implicit single-source maps, interface-based map registration, explicit multi-source maps, nested mapping, collection mapping, DI resolvers, value converters, conditional mapping, validation, first-pass projection mapping, first-pass plan diagnostics, tests, sample project, and DynaBee-backed generation are implemented.

Upcoming areas include:

- broader expression support
- deeper projection support
- richer collection destination support
- item converters for collection members
- richer diagnostics
- benchmarks against manual mapping and AutoMapper
