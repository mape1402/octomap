# Dynabee invoker requirements for OctoMap

## Status

Implemented by Dynabee `1.2.1` and integrated by OctoMap through `IDynaBeeBoundMethodInvoker` and `CreateBoundMethodInvoker(...)`.

## Purpose

OctoMap needs Dynabee to own generated method invocation the same way Dynabee already owns generated type creation and method body generation.

OctoMap currently uses Dynabee to:

- create generated mapper types
- create generated mapper instances
- generate mapper method bodies through `EmitsBody(...)`

The remaining gap is runtime invocation when OctoMap knows the source and destination types only at runtime.

OctoMap should not use `MethodInfo.Invoke(...)` or build expression-tree bridge delegates for generated mapper methods. Dynabee should expose a public invoker API that creates and caches efficient method invokers for generated types.

## Current OctoMap scenarios

### Single-source typed path

This path is already efficient because OctoMap can cast to the generated interface:

```csharp
var mapper = (IOctoMapper<TSource, TDestination>)compiledMap.Mapper;
return mapper.Map(source, context);
```

No Dynabee invoker is required for this path.

### Single-source runtime path

This path happens when the caller uses:

```csharp
mapper.Map<TDestination>(object source);
```

OctoMap knows the source type only at runtime:

```csharp
var sourceType = source.GetType();
var destinationType = typeof(TDestination);
```

The generated mapper method shape is:

```csharp
public TDestination Map(TSource source, IMapContext context);
```

OctoMap needs a fast invoker for that method without using `MethodInfo.Invoke(...)`.

### Multi-source runtime path

This path happens when the caller uses:

```csharp
mapper.Map<TDestination>(SourceSet.Of(order, customer));
```

The generated mapper method shape is:

```csharp
public TDestination Map(Order source0, Customer source1, IMapContext context);
```

OctoMap needs a fast invoker for an arbitrary number of source parameters plus `IMapContext`.

## Required Dynabee feature: generated method invokers

Dynabee should expose a public abstraction that represents a generated method invoker.

Suggested shape:

```csharp
public interface IDynaBeeMethodInvoker
{
    Type ReturnType { get; }

    IReadOnlyList<Type> ParameterTypes { get; }

    object Invoke(object instance, IReadOnlyList<object> arguments);
}
```

The exact name is flexible. The important part is that OctoMap can ask Dynabee for an invoker and then call it without knowing how Dynabee dispatches internally.

## Required factory API

Dynabee should allow creating an invoker from an `IAssemblyContext`.

Suggested API:

```csharp
public static class AssemblyContextInvokerExtensions
{
    public static IDynaBeeMethodInvoker CreateMethodInvoker(
        this IAssemblyContext context,
        string typeName,
        string methodName,
        IReadOnlyList<Type> parameterTypes);
}
```

Or directly on `IAssemblyContext` if that is acceptable:

```csharp
public interface IAssemblyContext
{
    IDynaBeeMethodInvoker CreateMethodInvoker(
        string typeName,
        string methodName,
        IReadOnlyList<Type> parameterTypes);
}
```

An extension method is less disruptive because it does not require changing the core `IAssemblyContext` contract.

## Preferred typed API

For cases where the caller knows the delegate shape, Dynabee should also expose typed delegate creation.

Suggested overloads:

```csharp
public static Func<T1, TResult> CreateMethodInvoker<T1, TResult>(
    this IAssemblyContext context,
    string typeName,
    string methodName);

public static Func<T1, T2, TResult> CreateMethodInvoker<T1, T2, TResult>(
    this IAssemblyContext context,
    string typeName,
    string methodName);

public static Func<T1, T2, T3, TResult> CreateMethodInvoker<T1, T2, T3, TResult>(
    this IAssemblyContext context,
    string typeName,
    string methodName);
```

OctoMap does not require the typed overloads to unblock the runtime path, but they would be useful for strongly typed integrations.

## Instance-bound invokers

Dynabee may also expose an instance-bound invoker:

```csharp
public interface IDynaBeeBoundMethodInvoker
{
    Type ReturnType { get; }

    IReadOnlyList<Type> ParameterTypes { get; }

    object Invoke(IReadOnlyList<object> arguments);
}
```

Suggested factory:

```csharp
public static IDynaBeeBoundMethodInvoker CreateBoundMethodInvoker(
    this IAssemblyContext context,
    string typeName,
    object instance,
    string methodName,
    IReadOnlyList<Type> parameterTypes);
```

This would let OctoMap store one compiled invoker per generated mapper instance:

```csharp
var mapper = context.CreateInstance(className);
var invoker = context.CreateBoundMethodInvoker(
    className,
    mapper,
    "Map",
    parameterTypes);
```

Then runtime invocation becomes:

```csharp
var result = invoker.Invoke(arguments);
```

This is the cleanest API for OctoMap because mapper instances are already cached inside `CompiledMap`.

## Invocation performance requirements

The invoker should avoid `MethodInfo.Invoke(...)` in the hot path.

Acceptable internal strategies:

- `Delegate.CreateDelegate(...)`
- expression-compiled delegate cached once
- generated bridge method
- dynamic method
- any future Dynabee-specific dispatch strategy

Not acceptable in the hot path:

- repeated method lookup
- repeated reflection invocation
- repeated expression compilation
- repeated delegate creation

Reflection during invoker construction is acceptable. Reflection during every map execution is not.

## Caching requirements

Dynabee should cache invokers by stable method identity:

```text
assembly context
type name
method name
parameter types
bound instance identity, only for bound invokers
```

If Dynabee chooses not to cache bound invokers because instance identity can grow unbounded, it should at least cache the unbound method dispatch plan and bind cheaply.

## Error behavior

Dynabee should fail early with clear exceptions when:

- the type name does not exist
- the method name does not exist
- no overload matches the provided parameter types
- more than one overload matches
- the instance is not assignable to the generated type
- an argument count mismatch occurs
- an argument cannot be assigned to the expected parameter type

The error should include:

- dynamic assembly name
- generated type name
- method name
- requested parameter types

## OctoMap integration target

After Dynabee exposes the invoker API, OctoMap should be able to build a compiled map like this:

```csharp
var mapper = context.CreateInstance(className);
var invoker = context.CreateBoundMethodInvoker(
    className,
    mapper,
    "Map",
    plan.SourceTypes.Concat(new[] { typeof(IMapContext) }).ToArray());

return new CompiledMap(
    mapper,
    mapper.GetType(),
    invoker);
```

The non-generic runtime path would call:

```csharp
compiledMap.Invoker.Invoke(new object[] { source, context });
```

The multi-source runtime path would call:

```csharp
compiledMap.Invoker.Invoke(
    sources.Sources.Concat(new object[] { context }).ToArray());
```

OctoMap would no longer need:

- `MethodInfo.Invoke(...)`
- runtime `GetMethod(...)`
- expression-compiled bridge delegates
- knowledge of invocation internals

## Acceptance criteria

Dynabee is ready for the next OctoMap cleanup when these scenarios pass without OctoMap using reflection invocation.

### Single-source runtime invoker

Given a generated mapper:

```csharp
public UserDto Map(User source, IMapContext context);
```

Dynabee can create a bound invoker and execute it:

```csharp
var invoker = context.CreateBoundMethodInvoker(
    "UserToUserDtoMapper",
    mapper,
    "Map",
    new[] { typeof(User), typeof(IMapContext) });

var result = invoker.Invoke(new object[] { user, mapContext });
```

### Multi-source runtime invoker

Given a generated mapper:

```csharp
public OrderDto Map(Order order, Customer customer, IMapContext context);
```

Dynabee can create a bound invoker and execute it:

```csharp
var invoker = context.CreateBoundMethodInvoker(
    "OrderCustomerToOrderDtoMapper",
    mapper,
    "Map",
    new[] { typeof(Order), typeof(Customer), typeof(IMapContext) });

var result = invoker.Invoke(new object[] { order, customer, mapContext });
```

### No repeated reflection invocation

A benchmark or test should prove that repeated calls through the invoker do not call `MethodInfo.Invoke(...)` each time.

## Proposed implementation order in Dynabee

1. Add public invoker abstractions.
2. Add `IAssemblyContext` extension methods for unbound and bound invokers.
3. Resolve methods by name and parameter types.
4. Create cached delegates internally.
5. Add clear error handling for missing and ambiguous methods.
6. Add tests for single-source and multi-source invokers.
7. Publish a new Dynabee version.
8. Return to OctoMap and replace runtime reflection/expression bridge invocation with Dynabee invokers.

## Non-goals

These are not required to unblock OctoMap:

- async invocation
- generic method invocation
- open generic generated types
- private method invocation
- constructor invokers
- property getter/setter invokers

## Summary

OctoMap needs Dynabee to expose method invokers so Dynabee owns not only generated type creation and method body generation, but also efficient runtime invocation of generated methods.

This keeps OctoMap focused on mapping configuration, planning, validation, and caching, while Dynabee remains the generation and dispatch engine.
