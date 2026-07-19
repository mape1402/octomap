# OctoMap

[![Build](https://github.com/mape1402/octomap/actions/workflows/publish.yaml/badge.svg)](https://github.com/mape1402/octomap/actions/workflows/publish.yaml)
[![NuGet](https://img.shields.io/nuget/v/OctoMap.svg)](https://www.nuget.org/packages/OctoMap)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

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
- Supports arrays, `List<T>`, `HashSet<T>`, sets, and common collection interface member mapping.
- Supports collection element conversion and first-pass collection projection.
- Supports DI-based value resolvers and value converters.
- Supports runtime lifecycle actions.
- Supports first-pass base map inclusion and polymorphic map lookup.
- Supports first-pass open generic map registration.
- Supports attribute-based map registration and member configuration.
- Supports configurable member naming conventions.
- Supports explicit registration organization through profiles, assembly scans, filters, duplicate policies, and configuration diagnostics.
- Provides first-pass analyzer tooling through `OctoMap.Analyzers`.
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

For larger applications, prefer the registration builder so profile scanning, interface/attribute scanning, options, and duplicate behavior are explicit.

```csharp
services.AddOctoMap(registration =>
{
    registration.Options.EnableRuntimeImplicitMaps = true;
    registration.Options.DuplicateMapPolicy = DuplicateMapPolicy.Throw;

    registration.AddMaps(typeof(SalesProfile).Assembly);
});
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

## Configuration Organization

`AddOctoMap(...)` supports a registration builder for applications that need clear startup composition.

```csharp
services.AddOctoMap(registration =>
{
    registration.Options.EnableRuntimeImplicitMaps = false;
    registration.Options.DuplicateMapPolicy = DuplicateMapPolicy.Throw;

    registration.AddProfile<SalesProfile>();
    registration.AddProfilesFromAssembly(typeof(BillingProfile).Assembly);
    registration.AddMaps(typeof(WarehouseItemDto).Assembly);
});
```

Registration methods:

- `AddProfile<TProfile>()`: registers one profile explicitly.
- `AddProfile(profile)`: registers a profile instance.
- `AddProfilesFromAssembly(assembly)`: discovers `OctoMapProfile` types only.
- `AddMaps(assembly)`: discovers profiles, `IMapFrom<T>`, `IMapTo<T>`, `[MapFrom]`, and `[MapTo]`.
- `WhereProfile(predicate)`: filters discovered profiles.
- `WhereMapType(predicate)`: filters interface and attribute map declaration types.

Duplicate explicit map declarations are controlled with `DuplicateMapPolicy`.

```csharp
registration.Options.DuplicateMapPolicy = DuplicateMapPolicy.Throw;
```

Policies:

- `Merge`: keeps the existing map and lets later configuration contribute to it.
- `Throw`: fails startup when the same explicit source/destination pair is declared twice.
- `Replace`: replaces the existing map with the later declaration.

Each map captures the active options when it is declared. This matters when profiles use different naming conventions or null-handling settings; later profile changes do not rewrite already-declared maps.

```csharp
public sealed class LegacyProfile : OctoMapProfile
{
    public override void Configure(IOctoMapConfigurationBuilder builder)
    {
        builder.UseSourceNamingConvention(SnakeCaseNamingConvention.Instance);
        builder.UseDestinationNamingConvention(PascalCaseNamingConvention.Instance);
        builder.CreateMap<LegacyOrder, LegacyOrderDto>();

        builder.UseSourceNamingConvention(ExactNamingConvention.Instance);
        builder.UseDestinationNamingConvention(ExactNamingConvention.Instance);
        builder.CreateMap<Product, ProductDto>();
    }
}
```

Loaded profiles and map declarations can be inspected from `IOctoMapConfiguration`.

```csharp
var configuration = provider.GetRequiredService<IOctoMapConfiguration>();

IReadOnlyList<string> profiles = configuration.GetProfiles();
string description = configuration.DescribeConfiguration();
```

`DescribeConfiguration()` lists loaded profiles, single-source maps, multi-source maps, and the declaration source for each single-source map. This diagnostic layer stays in OctoMap and does not expose DynaBee internals.

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
- `IgnoreNullSourceValue(...)`: skips assignment when the resolved source value is null.
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

## Existing Destination Mapping

Use the two-argument `Map(...)` overload when the destination instance already exists and should be updated in place.

```csharp
var existing = new CustomerDto
{
    InternalCode = "preserved"
};

var result = mapper.Map(customer, existing);

ReferenceEquals(existing, result); // true
```

Existing destination mapping uses the same single-source runtime mapping plan as normal object creation, but the generated method skips destination construction and assigns into the supplied instance. Ignored members and skipped conditional members keep their previous destination values. `ForPath(...)` reuses existing intermediate destination objects and creates missing ones when their types are supported.

This first pass supports single-source maps. Multi-source maps still require explicit creation because OctoMap intentionally avoids implicit multi-source behavior.

## Null Source Value Handling

Use `IgnoreNullSourceValue()` when a null source value should preserve the current destination value.

```csharp
builder.CreateMap<CustomerPatch, CustomerDto>()
    .ForMember(x => x.FullName, x => x.IgnoreNullSourceValue());

mapper.Map(new CustomerPatch { FullName = null }, existingCustomerDto);
```

You can also enable the behavior globally.

```csharp
services.AddOctoMap(
    options => options.IgnoreNullSourceValues = true,
    typeof(SalesProfile).Assembly);
```

Per-member configuration wins over the global option:

```csharp
builder.CreateMap<CustomerPatch, CustomerDto>()
    .ForMember(x => x.FullName, x => x.IgnoreNullSourceValue(false));
```

`NullSubstitute(...)` runs before the null-skip check, so an explicit substitute value is assigned even when null source values are ignored.

## Global Type Conversions

OctoMap can convert matching source and destination members when the member names match but their types differ.

Built-in conversions include:

- numeric-to-numeric conversions
- nullable wrap and unwrap conversions
- enum-to-string and string-to-enum
- string-to-`Guid`
- string-to-date/time types supported by their `Parse(...)` APIs
- primitive/date/time/Guid-to-string conversions

Register reusable projectable conversions with an expression:

```csharp
builder.CreateConverter<string, SkuCode>(x => new SkuCode(x.ToUpperInvariant()));
builder.CreateMap<Product, ProductCodeDto>();
```

Register runtime conversions through DI when conversion needs services:

```csharp
builder.CreateConverter<MoneyTextConverter, decimal, MoneyText>();
```

DI converters are resolved from `IMapContext.Services` on each map call, so scoped/transient/singleton lifetimes stay controlled by the application service provider.

Expression converters can be used by `ProjectTo(...)`. DI converters are runtime-only and throw a clear projection error.

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

## Naming Conventions

Naming conventions let convention maps match members that use different casing or separators.

```csharp
services.AddOctoMap(options =>
{
    options.SourceNamingConvention = SnakeCaseNamingConvention.Instance;
    options.DestinationNamingConvention = PascalCaseNamingConvention.Instance;
}, typeof(SalesProfile).Assembly);
```

The same configuration can be declared from a profile:

```csharp
public sealed class SalesProfile : OctoMapProfile
{
    public override void Configure(IOctoMapConfigurationBuilder builder)
    {
        builder.UseSourceNamingConvention(SnakeCaseNamingConvention.Instance);
        builder.UseDestinationNamingConvention(PascalCaseNamingConvention.Instance);

        builder.CreateMap<LegacyOrder, LegacyOrderDto>();
    }
}
```

This maps:

```text
customer_name -> CustomerName
order_total   -> OrderTotal
```

Supported built-in conventions:

- `ExactNamingConvention`
- `PascalCaseNamingConvention`
- `CamelCaseNamingConvention`
- `SnakeCaseNamingConvention`
- `KebabCaseNamingConvention`

`ExactNamingConvention` is the default, so existing maps keep the previous exact-name behavior. You can also remove simple source prefixes or destination suffixes before matching:

```csharp
builder.RecognizeSourcePrefixes("m_");
builder.RecognizeSourceSuffixes("_field");
builder.RecognizeDestinationPrefixes("View");
builder.RecognizeDestinationSuffixes("Dto");
```

Naming conventions are used by convention member matching, convention constructor mapping, and flattening. Explicit configuration through `ForMember(...)`, `ForPath(...)`, attributes, resolvers, and converters still wins.

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

`ReverseMap()` can also reverse explicit `ForPath(...)` unflattening when the original source is a direct property.

```csharp
builder.CreateMap<OrderDto, Order>()
    .ForPath(x => x.Customer.FirstName, x => x.MapFrom(s => s.CustomerFirstName))
    .ReverseMap();
```

This maps `Order.Customer.FirstName` back to `OrderDto.CustomerFirstName`.

## Lifecycle Actions

Lifecycle actions run only in runtime mapping. They are not projectable because LINQ providers cannot translate service calls or arbitrary callbacks.

Use inline actions for local behavior:

```csharp
builder.CreateMap<Order, OrderDto>()
    .BeforeMap((source, destination, context) => destination.Status = "mapping")
    .AfterMap((source, destination, context) => destination.Status = destination.Status.Trim());
```

Use DI-backed actions when behavior needs services:

```csharp
public sealed class OrderAuditAction : IMappingAction<Order, OrderDto>
{
    private readonly IAuditLog _auditLog;

    public OrderAuditAction(IAuditLog auditLog)
    {
        _auditLog = auditLog;
    }

    public void Process(Order source, OrderDto destination, IMapContext context)
        => _auditLog.WriteMappedOrder(source.Id);
}

builder.CreateMap<Order, OrderDto>()
    .AfterMap<OrderAuditAction>();
```

Register DI-backed actions and their dependencies in the application service provider. OctoMap resolves action services on each map call, so scoped, transient, and singleton lifetimes remain controlled by DI.

## Inheritance and Polymorphism

`IncludeBase<TBaseSource, TBaseDestination>()` copies explicit member configuration from a configured base map into a derived map.

```csharp
builder.CreateMap<BaseOrder, BaseOrderDto>()
    .ForMember(x => x.StatusLabel, x => x.MapFrom(s => s.StatusCode.ToUpperInvariant()));

builder.CreateMap<OnlineOrder, OnlineOrderDto>()
    .IncludeBase<BaseOrder, BaseOrderDto>();
```

OctoMap can also resolve a configured base map when the runtime source type is derived and the requested destination is compatible with the configured base destination.

```csharp
BaseOrder source = new OnlineOrder { StatusCode = "ready" };
var dto = mapper.Map<BaseOrderDto>(source);
```

This is a first pass. Ambiguous inheritance maps should still be configured explicitly.

## Open Generic Maps

Open generic maps can be registered with the non-generic `CreateMap(...)` API.

```csharp
builder.CreateMap(typeof(Box<>), typeof(BoxDto<>));
builder.CreateMap(typeof(Page<>), typeof(PageDto<>));
```

OctoMap closes the map lazily when a closed generic pair is requested.

```csharp
var dto = mapper.Map<Box<Customer>, BoxDto<CustomerDto>>(box);
```

The closed generic map then uses the same runtime planning, validation, nested mapping, collection mapping, and cache behavior as a normal map.

## Projection Mapping

OctoMap can build LINQ projection expressions for query providers such as Entity Framework.

```csharp
var mapper = provider.GetRequiredService<IOctoMapper>();

IQueryable<ProductDto> query = db.Products
    .ProjectTo<ProductDto>(mapper.ProjectionBuilder);
```

`ProjectTo(...)` uses `mapper.ProjectionBuilder` to build an `Expression<Func<TSource, TDestination>>` behind the scenes. Projection is intentionally separate from the DynaBee runtime backend because LINQ providers need expression trees they can translate.

The first projection pass supports:

- direct property mapping
- configured `MapFrom(...)` expressions
- constants
- null substitutes
- convention flattening
- constructor projection for records and immutable DTOs
- collection projection for assignable elements and expression-based element conversions

Runtime-only features are rejected with clear errors:

- DI resolvers
- DI value converters
- DI collection element converters
- conditional mapping
- nested runtime mapping
- nested collection element runtime mapping
- lifecycle actions
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
- `HashSet<T>`
- `IEnumerable<T>`
- `ICollection<T>`
- `IReadOnlyCollection<T>`
- `IList<T>`
- `IReadOnlyList<T>`
- `ISet<T>`
- `IReadOnlySet<T>`

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

OctoMap generates a loop through DynaBee. If the item type needs a map, OctoMap uses the cached item mapper per element. If the item type is already assignable, OctoMap copies the item value/reference. If a global type conversion exists for the element pair, OctoMap applies that conversion per item before falling back to nested item mapping.

```csharp
builder.CreateConverter<string, SkuCode>(x => new SkuCode(x.ToUpperInvariant()));

public sealed class Order
{
    public IEnumerable<string> Tags { get; set; }
}

public sealed class OrderDto
{
    public IReadOnlySet<SkuCode> Tags { get; set; }
}
```

Expression-based element conversions can also be used by `ProjectTo(...)`. DI-based element converters are runtime-only.

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

## Attribute-Based Registration

Attributes are convenience configuration for simple maps. Fluent configuration still wins when both configure the same destination member.

Use `[MapFrom]` or `[MapTo]` to register maps during assembly scanning:

```csharp
[MapFrom(typeof(Order))]
public sealed class OrderDto
{
    [MapName("StatusCode")]
    public string Status { get; set; }

    [NullSubstitute("No description")]
    public string Description { get; set; }

    [IgnoreMap]
    public string InternalCode { get; set; }
}
```

Use `[MapConstructor]` to prefer a constructor for constructor mapping:

```csharp
public sealed class OrderDto
{
    [MapConstructor]
    public OrderDto(int id, string status)
    {
        Id = id;
        Status = status;
    }
}
```

Attribute configuration is visible through the normal planning and diagnostics flow because attributes are converted into regular OctoMap configuration during startup.

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
Existing destination map: same instance True - Grace Hopper - internal 'preserved'
Ignore null source value patch: Grace Hopper
Plan description: CustomerDto.Id <- Customer.Id
Configuration diagnostics: 1 profile loaded
Constructor map: 100 - Grace Hopper
Implicit map: OCTO-001 - 49.95
Reverse map: OCTO-001 - 49.95
Global type converter map: OCTO-CODE
Naming convention map: Legacy Ada - 88.50
Projection map: OCTO-PROJ - 19.95
EF SQLite projection map: OCTO-SQLITE - 29.95
Resolver, value converter, nested map, collection map, flattening: 700 - NEW - No description - Order #0700 is Created - 149.99 USD - Katherine Johnson - Katherine - 2 items - 2 converted tags
ForPath map: 800 - Dorothy
Generated expressions: OCTO-HOODIE - 109.97 - remainder 1 - can ship True
Conditional map: skipped
IMapFrom interface map: WH-42
IMapTo interface map: SHIP - Dock 7
Attribute map: ATTR - No attributed description - internal 'ignored'
Multi-source map: 701 - Ada - Priority order - Ada
```

## Development

Analyzer documentation lives in [docs/ANALYZERS.md](docs/ANALYZERS.md).

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

OctoMap is in early alpha. The core runtime path, explicit maps, runtime implicit single-source maps, naming conventions, interface-based map registration, attribute-based registration, explicit multi-source maps, nested mapping, collection mapping, collection element conversion, DI resolvers, value converters, lifecycle actions, conditional mapping, inheritance map inclusion, open generic map resolution, validation, first-pass projection mapping, first-pass plan diagnostics, tests, sample project, and DynaBee-backed generation are implemented.

Upcoming areas include:

- broader expression support
- deeper projection support
- richer collection update and merge policies
- richer diagnostics
- benchmarks against manual mapping and AutoMapper
