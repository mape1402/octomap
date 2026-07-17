# Dynabee method body requirements for DI-based resolvers

## Status

Implemented by Dynabee `1.2.2` and integrated by OctoMap through `Self()`, `Call(...)`, and `StaticCall(...)`.

## Purpose

OctoMap needs to support mapping members through services resolved from dependency injection. The requested OctoMap feature is resolver-based mapping, but the Dynabee feature should remain generic: generated method bodies need to access the generated instance and call instance/static methods through the public body builder API.

This is useful beyond OctoMap for generated services, proxies, adapters, validators, serializers, command handlers, and any generated type that needs to call collaborators.

## OctoMap use case

OctoMap wants to support:

```csharp
builder.CreateMap<Order, OrderDto>()
    .ForMember(x => x.TotalText, x => x.ResolveUsing<OrderTotalTextResolver>());
```

Resolver:

```csharp
public sealed class OrderTotalTextResolver
    : IValueResolver<Order, OrderDto, string>
{
    public string Resolve(Order source, OrderDto destination, IMapContext context)
        => source.Total.ToString("C");
}
```

Generated mapper shape, conceptually:

```csharp
public sealed class OrderToOrderDtoMapper : IOctoMapper<Order, OrderDto>
{
    public OrderDto Map(Order source, IMapContext context)
    {
        var destination = new OrderDto();

        var resolver = context.Services.GetRequiredService<OrderTotalTextResolver>();
        destination.TotalText = resolver.Resolve(source, destination, context);

        return destination;
    }
}
```

OctoMap may choose constructor-injected `IServiceProvider` later, but a per-call provider available through `IMapContext` preserves scoped lifetimes more naturally with cached generated mappers.

## Required generic Dynabee capability: method calls

The body builder should expose method call expressions.

Suggested API:

```csharp
IBeeValueExpression Call(
    IBeeValueExpression instance,
    MethodInfo method,
    params IBeeValueExpression[] arguments);

IBeeValueExpression StaticCall(
    MethodInfo method,
    params IBeeValueExpression[] arguments);
```

String-based overloads may be useful:

```csharp
IBeeValueExpression Call(
    IBeeValueExpression instance,
    string methodName,
    IReadOnlyList<Type> parameterTypes,
    params IBeeValueExpression[] arguments);
```

The API should support:

- instance method calls
- static method calls
- generic methods after the caller supplies a closed `MethodInfo`
- void method calls as statements if Dynabee supports statement calls
- return value calls as expressions

## Required generic Dynabee capability: generated self

If integrations choose constructor/property injection, the body builder also needs access to the generated instance.

Suggested API:

```csharp
IBeeValueExpression Self();
```

Usage:

```csharp
var services = body.Property(body.Self(), "Services");
```

This is not OctoMap-specific. It lets any generated method read properties or fields from its own generated type.

## Required generic Dynabee capability: assignable method result

Dynabee should allow assigning a method call result to a destination member:

```csharp
body.Assign(
    body.Property(destination, "TotalText"),
    body.Call(resolver, resolveMethod, source, destination, context));
```

This requires method calls to implement `IBeeValueExpression`.

## Required generic Dynabee capability: interface method calls

OctoMap resolvers will typically be called through an interface:

```csharp
IValueResolver<TSource, TDestination, TMember>.Resolve(...)
```

Dynabee should emit the correct call instruction for interface calls.

The body builder should validate:

- method belongs to the instance type or an implemented interface
- argument count matches
- argument types can be assigned or converted
- return type is known and can be assigned by the caller

## Required generic Dynabee capability: service provider calls

OctoMap can avoid needing special Dynabee support for `Microsoft.Extensions.DependencyInjection` if Dynabee supports generic static method calls.

Example:

```csharp
var getRequiredService = typeof(ServiceProviderServiceExtensions)
    .GetMethods()
    .Single(x => x.Name == nameof(ServiceProviderServiceExtensions.GetRequiredService)
        && x.IsGenericMethodDefinition
        && x.GetParameters().Length == 1)
    .MakeGenericMethod(typeof(OrderTotalTextResolver));

var resolver = body.StaticCall(getRequiredService, services);
```

This remains generic: Dynabee only emits a static method call. OctoMap decides the method.

## Scoped lifetime correctness

OctoMap must not cache resolver instances. The service provider controls lifetimes:

- transient services may produce new instances
- scoped services must resolve from the active scope
- singleton services are reused by the provider

Dynabee does not need to know about those lifetimes. It only needs to emit calls that OctoMap requests.

## Acceptance criteria

Dynabee is ready for OctoMap resolver integration when the following can be generated without direct IL in OctoMap.

### Instance method call

```csharp
public string Map(Source source)
{
    var helper = new Helper();
    return helper.Format(source.Name);
}
```

### Static generic method call

```csharp
public object Resolve(IServiceProvider services)
{
    return services.GetRequiredService<MyService>();
}
```

### Interface method call

```csharp
public string Resolve(IValueResolver<Source, Destination, string> resolver, Source source, Destination destination, IMapContext context)
{
    return resolver.Resolve(source, destination, context);
}
```

### Self property access

```csharp
public string Resolve(Source source)
{
    return this.Formatter.Format(source.Name);
}
```

## Proposed implementation order in Dynabee

1. Add `Self()` to `IBeeMethodBodyBuilder`.
2. Add instance `Call(...)` to `IBeeMethodBodyBuilder`.
3. Add static `StaticCall(...)` to `IBeeMethodBodyBuilder`.
4. Support closed generic `MethodInfo` calls.
5. Support interface method calls.
6. Add validation errors for incompatible method calls.
7. Add tests for call results assigned to properties.
8. Publish a new Dynabee version.
9. Return to OctoMap and implement resolver-based member mapping.

## Non-goals

These are not required to unblock OctoMap:

- automatic dependency injection integration inside Dynabee
- service lifetime handling
- resolver-specific APIs
- expression tree parsing for resolver calls
- async method calls
- open generic method invocation

## Summary

OctoMap needs DI-based resolvers, but Dynabee should only add generic method body capabilities:

- access to `self`
- instance method calls
- static method calls
- interface method calls

With those primitives, OctoMap can generate resolver-based mapping while leaving service lifetimes to the application DI container.
