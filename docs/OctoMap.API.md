# OctoMap API Reference

## Registration

```csharp
services.AddOctoMap(params Assembly[] profileAssemblies);

services.AddOctoMap(
    Action<OctoMapOptions> configureOptions,
    params Assembly[] profileAssemblies);
```

`AddOctoMap` registers OctoMap services, discovers profiles, discovers `IMapFrom<T>` / `IMapTo<T>` maps, and registers the DynaBee-backed generation backend.

## Profiles

Profiles group mapping configuration.

```csharp
public abstract class OctoMapProfile
{
    public abstract void Configure(IOctoMapConfigurationBuilder builder);
}
```

Example:

```csharp
public sealed class SalesProfile : OctoMapProfile
{
    public override void Configure(IOctoMapConfigurationBuilder builder)
    {
        builder.CreateMap<Customer, CustomerDto>();
    }
}
```

## Single-Source Maps

```csharp
IMapExpression<TSource, TDestination> CreateMap<TSource, TDestination>();
void CreateMap(Type sourceType, Type destinationType);
```

Single-source maps support explicit member rules, destination construction rules, and convention matching by destination/source property name.

```csharp
builder.CreateMap<Customer, CustomerDto>()
    .ForMember(x => x.FullName, x => x.MapFrom(s => s.FirstName + " " + s.LastName))
    .ForMember(x => x.InternalCode, x => x.Ignore());
```

### Reverse Maps

```csharp
IMapExpression<TDestination, TSource> ReverseMap();
```

`ReverseMap()` registers a basic reverse single-source map and returns its expression for additional configuration.

```csharp
builder.CreateMap<Product, ProductDto>()
    .ReverseMap();
```

Reverse maps support convention member matching and direct reversible `MapFrom(...)` rules. Resolvers, converters, flattening, unflattening, complex expressions, and multi-source maps are not reversed automatically.

### Member Rules

```csharp
void Ignore();
void MapFrom(Expression<Func<TSource, TMember>> sourceExpression);
void PreCondition(Expression<Func<TSource, bool>> conditionExpression);
void Condition(Expression<Func<TSource, bool>> conditionExpression);
void Condition(Expression<Func<TSource, TMember, bool>> conditionExpression);
void ResolveUsing<TResolver>()
    where TResolver : IValueResolver<TSource, TDestination, TMember>;
void UseValue(TMember value);
void NullSubstitute(TMember value);
```

### Conditional Members

```csharp
builder.CreateMap<OrderLine, OrderLineDto>()
    .ForMember(x => x.DisplayName, x =>
    {
        x.MapFrom(s => s.DisplayName);
        x.PreCondition(s => s.IsActive);
        x.Condition((source, value) => value != "skip");
    });
```

`PreCondition(...)` runs before value resolution. `Condition(...)` runs after value resolution and before assignment. Runtime mapping supports conditional members for direct members, resolvers, converters, nested maps, collection maps, destination paths, and source contributions in multi-source maps. Projection support for conditional mapping is not implemented yet.

### Destination Paths

```csharp
IMapExpression<TSource, TDestination> ForPath<TMember>(
    Expression<Func<TDestination, TMember>> destinationPath,
    Action<IMemberMapExpression<TSource, TDestination, TMember>> configure);
```

`ForPath(...)` configures a nested destination member path.

```csharp
builder.CreateMap<OrderDto, Order>()
    .ForPath(x => x.Customer.Name, x => x.MapFrom(s => s.CustomerName));
```

Intermediate destination members are created when they are null and their types expose public parameterless constructors. `ForPath(...)` supports the same member rules as `ForMember(...)` for runtime mapping. Nested destination path projection is not supported yet.

### Destination Construction

Destinations with public parameterless constructors are created by convention. If a destination does not expose a public parameterless constructor, OctoMap can match public constructor parameters to readable source properties by name.

```csharp
public sealed record CustomerDto(int Id, string Name);

builder.CreateMap<Customer, CustomerDto>();
```

Construction can also be configured explicitly.

```csharp
IMapExpression<TSource, TDestination> ConstructUsing(
    Expression<Func<TSource, TDestination>> constructionExpression);

builder.CreateMap<Customer, CustomerDto>()
    .ConstructUsing(s => new CustomerDto(s.Id, s.FirstName + " " + s.LastName));
```

After construction, OctoMap still applies settable destination member assignments that were not already supplied through the constructor.

`ResolveUsing<TResolver>()` resolves `TResolver` from `IMapContext.Services` on each map call and invokes:

```csharp
TMember Resolve(TSource source, TDestination destination, IMapContext context);
```

Resolver contract:

```csharp
public interface IValueResolver<TSource, TDestination, TMember>
{
    TMember Resolve(TSource source, TDestination destination, IMapContext context);
}
```

## Runtime Implicit Maps

Runtime implicit maps can be enabled for single-source maps.

```csharp
services.AddOctoMap(options => options.EnableRuntimeImplicitMaps = true);
```

When enabled, `mapper.Map<TDestination>(source)` can create and cache a convention map at runtime.

Runtime implicit maps do not apply to multi-source maps.

## Existing Destination Mapping

```csharp
public interface IOctoMapper
{
    TDestination Map<TSource, TDestination>(
        TSource source,
        TDestination destination);
}
```

The existing destination overload updates the supplied destination instance and returns the same instance.

```csharp
var result = mapper.Map(customer, existingCustomerDto);
```

Supported behavior:

- single-source maps
- configured maps and runtime implicit maps
- direct members, nested maps, collections, converters, resolvers, null substitutes, conditions, and `ForPath(...)`
- reuse of existing nested destination instances when a nested map is applied
- creation of missing `ForPath(...)` intermediates when their types can be constructed

Multi-source existing destination mapping is intentionally not exposed yet.

## Projection Mapping

```csharp
public interface IOctoProjectionBuilder
{
    Expression<Func<TSource, TDestination>> Build<TSource, TDestination>();
}

public interface IOctoMapper
{
    IQueryable<TDestination> ProjectTo<TSource, TDestination>(
        IQueryable<TSource> source);
}

public static IQueryable<TDestination> ProjectTo<TSource, TDestination>(
    this IQueryable<TSource> source,
    IOctoMapper mapper);
```

`IOctoMapper.ProjectTo(...)` builds provider-friendly projection expressions from OctoMap configuration and applies them through `Queryable.Select(...)`. `IOctoProjectionBuilder` is also available as the lower-level projection abstraction.

```csharp
var mapper = provider.GetRequiredService<IOctoMapper>();

var query = db.Products.ProjectTo<Product, ProductDto>(mapper);
```

Projection support is single-source. It can use configured maps or runtime implicit maps when `EnableRuntimeImplicitMaps` is enabled.

Supported projection features:

- direct property mapping
- configured `MapFrom(...)` expressions
- constants
- null substitutes
- flattening
- convention constructor projection
- explicit constructor projection when `ConstructUsing(...)` uses a `new` expression

Unsupported projection features throw `NotSupportedException`:

- DI resolvers
- DI value converters
- nested runtime mapping
- collection runtime mapping
- multi-source maps

## Flattening

Single-source maps support flattened destination members by convention.

```csharp
public sealed class OrderDto
{
    public string CustomerName { get; set; }
}

builder.CreateMap<Order, OrderDto>();
```

OctoMap can map `Order.Customer.Name` to `OrderDto.CustomerName` when the source path is readable and the final source value is assignable to the destination member. Intermediate null source objects produce the destination member default value. Flattening is not applied to multi-source maps.

## Multi-Source Maps

```csharp
IMultiMapExpression<TDestination> CreateMultiMap<TDestination>();
```

Multi-source maps create one destination from multiple configured source types.

```csharp
builder.CreateMultiMap<OrderSummaryDto>()
    .From<Order>(map => map
        .ForMember(x => x.OrderId, x => x.MapFrom(s => s.Id)))
    .From<Customer>(map => map
        .ForMember(x => x.CustomerName, x => x.MapFrom(s => s.Name)))
    .ForMember(x => x.Label, x => x.MapFrom(ctx =>
        ctx.Get<Order>().Code + " - " + ctx.Get<Customer>().Name));
```

### Source Contributions

```csharp
IMultiMapExpression<TDestination> From<TSource>(
    Action<IMapExpression<TSource, TDestination>> configure);
```

Each `.From<TSource>(...)` block configures destination members contributed by one source type.

### Multi-Source Context Members

```csharp
IMultiMapExpression<TDestination> ForMember<TMember>(
    Expression<Func<TDestination, TMember>> destinationMember,
    Action<IMultiSourceMemberMapExpression<TDestination, TMember>> configure);

void MapFrom(Expression<Func<IMultiSourceMapContext, TMember>> sourceExpression);
```

Use `ctx.Get<TSource>()` when a destination member needs data from more than one source.

```csharp
.ForMember(x => x.Label, x => x.MapFrom(ctx =>
    ctx.Get<Order>().Code + " - " + ctx.Get<Customer>().Name));
```

### Runtime Source Set

```csharp
var dto = mapper.Map<OrderSummaryDto>(SourceSet.Of(order, customer));
```

`SourceSet.Get<TSource>()` returns the matching source instance. It throws when no matching source exists or when more than one source is assignable to `TSource`.

### Multi-Source Rules

- Multi-source maps must be configured explicitly.
- Multi-source maps are not created implicitly at runtime.
- Multi-source maps do not use convention matching.
- Every destination member must be mapped explicitly.
- The same destination member cannot be configured more than once.
- `ctx.Get<TSource>()` must resolve to exactly one configured source.

## Mapper

```csharp
public interface IOctoMapper
{
    TDestination Map<TDestination>(object source);
    TDestination Map<TSource, TDestination>(TSource source);
    TDestination Map<TDestination>(SourceSet sources);
    IQueryable<TDestination> ProjectTo<TSource, TDestination>(IQueryable<TSource> source);
}
```

Typed single-source maps use generated mapper interfaces. Runtime single-source and multi-source maps use DynaBee generated method invokers.

## Plan Diagnostics

```csharp
public interface IOctoMapConfiguration
{
    MappingPlan GetPlan<TSource, TDestination>();
    MappingPlan GetPlan(Type sourceType, Type destinationType);
    MappingPlan GetPlan(IReadOnlyList<Type> sourceTypes, Type destinationType);
    string DescribeMap<TSource, TDestination>();
    string DescribeMap(Type sourceType, Type destinationType);
    string DescribeMap(IReadOnlyList<Type> sourceTypes, Type destinationType);
}
```

`GetPlan(...)` returns the executable mapping plan after validation. `DescribeMap(...)` returns a human-readable assignment report.

```csharp
var description = configuration.DescribeMap<Order, OrderDto>();
```

Example output:

```text
OrderDto.Id <- Order.Id
OrderDto.CustomerName <- Order.Customer.Name
OrderDto.TotalText <- OrderTotalTextConverter(Order.Total)
```

The default plan describer is registered as `IMappingPlanDescriber`, so diagnostic formatting can be replaced without changing the mapping runtime.

## Interface-Based Registration

```csharp
public interface IMapFrom<TSource>
{
}

public interface IMapTo<TDestination>
{
}
```

Types implementing these interfaces are discovered when their assemblies are passed to `AddOctoMap(...)`.
