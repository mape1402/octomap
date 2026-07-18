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

### Member Rules

```csharp
void Ignore();
void MapFrom(Expression<Func<TSource, TMember>> sourceExpression);
void ResolveUsing<TResolver>()
    where TResolver : IValueResolver<TSource, TDestination, TMember>;
void UseValue(TMember value);
void NullSubstitute(TMember value);
```

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
}
```

Typed single-source maps use generated mapper interfaces. Runtime single-source and multi-source maps use DynaBee generated method invokers.

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
