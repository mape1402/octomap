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
- `UseValue(...)`: assigns a constant value.
- `NullSubstitute(...)`: replaces null source results for reference-type destination members.
- `Ignore()`: excludes a destination member.

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
Implicit map: OCTO-001 - 49.95
Value rules: 700 - Created - No description
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

OctoMap is in early alpha. The core runtime path, explicit maps, runtime implicit single-source maps, interface-based map registration, explicit multi-source maps, validation, tests, sample project, and DynaBee-backed generation are implemented.

Upcoming areas include:

- converters and resolvers
- nested mapping composition
- broader expression support
- richer diagnostics
- benchmarks against manual mapping and AutoMapper
