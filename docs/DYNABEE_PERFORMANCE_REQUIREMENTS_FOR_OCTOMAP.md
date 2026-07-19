# Dynabee performance requirements for OctoMap

## Status

Requested after OctoMap moved generated mapper hot paths away from reflection and into direct generated interfaces.

This document describes the next generic Dynabee capabilities that would let OctoMap remove the remaining adapter overhead around generated methods. The requested features are intentionally not OctoMap-specific. They are useful for serializers, validators, proxy generators, DTO mappers, import/export pipelines, and any runtime generation library that needs fast generated dispatch.

## Current OctoMap state

OctoMap now asks Dynabee to generate mapper classes that implement OctoMap-owned interfaces such as:

```csharp
public interface IOctoMapping<TSource, TDestination>
{
    TDestination Map(TSource source, IMapContext context);
}
```

For single-source maps, the warm runtime path can call the generated mapper through this direct interface. That removed the biggest runtime reflection cost.

There are still performance costs around generation and dispatch:

- OctoMap still creates some bridge objects with `MakeGenericMethod(...).Invoke(...)` during compilation.
- Dynabee bound invokers still use `object[]` for dynamic source-set invocation.
- Generated types cannot expose all natural overloads with the same method name, so OctoMap uses auxiliary names such as `MapContextFree`.
- OctoMap must still build adapter delegates around generated instances for some object-based and fallback paths.
- Multi-source typed paths above two sources do not yet have typed generated invoker helpers.

These are not correctness blockers, but they limit how close OctoMap can get to hand-written mapper performance.

## Design boundary

OctoMap should continue to own:

- mapping configuration
- mapping plan creation
- validation and diagnostics
- runtime cache policy
- service lifetime decisions
- public mapper abstractions

Dynabee should continue to own:

- runtime type generation
- generated method body emission
- generated instance creation
- efficient generated method dispatch
- generic delegate binding over generated methods

Dynabee should not add OctoMap-specific mapping concepts.

## Required feature 1: strongly typed bound delegates

Dynabee should expose a way to create strongly typed delegates for generated instance methods.

Illustrative API:

```csharp
public interface IDynaBeeAssemblyContext
{
    TDelegate CreateBoundDelegate<TDelegate>(
        string className,
        object instance,
        string methodName,
        Type[] parameterTypes)
        where TDelegate : Delegate;
}
```

Example usage:

```csharp
var map = context.CreateBoundDelegate<Func<Order, IMapContext, OrderDto>>(
    className,
    mapper,
    "Map",
    new[] { typeof(Order), typeof(IMapContext) });
```

Dynabee should validate that:

- `TDelegate` is a delegate type.
- The delegate return type matches the generated method return type.
- The delegate parameters match the generated method parameters.
- The target method exists unambiguously.
- The generated method can be called without `MethodInfo.Invoke`.

### Why this is generic

Any consumer that generates a type and needs fast method execution benefits from typed delegate binding. This is not a mapper feature; it is generated dispatch infrastructure.

### OctoMap impact

OctoMap can remove reflection-based compile-time helper calls such as `MakeGenericMethod(...).Invoke(...)` when creating object and typed bridge delegates. Warm map execution is already mostly direct, but this also improves cold compile and fallback paths.

## Required feature 2: typed open delegate creation

Dynabee should also support open delegates where the generated instance remains an explicit first parameter.

Illustrative API:

```csharp
public interface IDynaBeeAssemblyContext
{
    TDelegate CreateOpenDelegate<TDelegate>(
        string className,
        string methodName,
        Type[] parameterTypes)
        where TDelegate : Delegate;
}
```

Example:

```csharp
var map = context.CreateOpenDelegate<Func<object, Order, IMapContext, OrderDto>>(
    className,
    "Map",
    new[] { typeof(Order), typeof(IMapContext) });
```

Dynabee can choose the exact first parameter type rules. It may require the generated class type, or it may support `object` and perform a generated or cached cast internally.

### Why this is generic

Open delegates are useful for dispatch caches, object pools, interceptors, proxies, generated validators, and any runtime-generated component where instances are selected independently from the method dispatch plan.

### OctoMap impact

OctoMap can cache method dispatch by generated type and bind cheaply to generated mapper instances. This helps dynamic `SourceSet` invocation and any future pooling or backend dispatch layer.

## Required feature 3: overload-safe method lookup

Dynabee should support generated classes with overloaded methods and provide overload-safe lookup for method body generation and delegate creation.

OctoMap would like generated classes to expose natural overloads:

```csharp
public OrderDto Map(Order source, IMapContext context);
public OrderDto Map(Order source);
public OrderDto Map(Order source, OrderDto destination, IMapContext context);
public OrderDto Map(Order source, OrderDto destination);
```

Today OctoMap uses auxiliary names such as:

```csharp
MapContextFree(...)
MapToExistingContextFree(...)
```

That works, but it leaks generation limitations into OctoMap's adapter code and creates extra branching.

Dynabee should allow:

- adding multiple methods with the same name when their parameter types differ
- selecting a method by `(name, parameterTypes)`
- creating invokers/delegates by `(name, parameterTypes)`
- clear errors when overloads are ambiguous

### Why this is generic

Overloads are a normal .NET method feature. Code generators for proxies, adapters, clients, model binders, validators, and serializers all need reliable overload support.

### OctoMap impact

OctoMap can simplify generated mapper contracts and reduce adapter method names. It also makes future generated interfaces and direct delegate generation cleaner.

## Required feature 4: generated instance factory delegates

Dynabee currently owns generated instance creation through `CreateInstance(...)`. For high-throughput scenarios, Dynabee should expose reusable factory delegates.

Illustrative API:

```csharp
public interface IDynaBeeAssemblyContext
{
    Func<object[], object> CreateFactory(string className);

    TFactory CreateFactory<TFactory>(string className)
        where TFactory : Delegate;
}
```

Examples:

```csharp
var objectFactory = context.CreateFactory(className);

var typedFactory = context.CreateFactory<Func<ICompiledMapInvoker<Order, OrderDto>, GeneratedMapper>>(
    className);
```

The typed factory shape can be constrained by Dynabee's supported constructor model. The important part is that Dynabee owns fast construction and caches constructor dispatch instead of every consumer inventing reflection or expression-tree factory code.

### Why this is generic

Generated type construction is useful beyond mapping: generated proxies, DTO materializers, validators, serializers, controllers, test doubles, and message handlers all need fast instance creation.

### OctoMap impact

OctoMap can remove remaining reflection-ish construction bridges and rely completely on Dynabee for generated object creation and constructor dispatch.

## Required feature 5: multi-arity typed invoker helpers

Dynabee's current bound invoker is useful for object-based dispatch, but OctoMap's multi-source maps need typed delegate support up to 10 sources.

Dynabee does not need OctoMap-specific interfaces. It only needs the typed delegate support from feature 1 to work for arbitrary arity:

```csharp
Func<TSource1, TSource2, IMapContext, TDestination>
Func<TSource1, TSource2, TSource3, IMapContext, TDestination>
...
Func<TSource1, ..., TSource10, IMapContext, TDestination>
```

The delegate API should not have an artificial arity limit lower than .NET delegate type limits.

### Why this is generic

Generated dispatch with multiple inputs is useful for validators, merge functions, aggregators, composite model binders, code-generated reducers, and state transition pipelines.

### OctoMap impact

OctoMap can cache direct typed delegates for multi-source maps from 3 to 10 sources instead of calling the registry and generated mapper through less specialized paths.

## Required feature 6: optional generated cast adapters

Dynabee could expose a generic way to generate small cast adapters around strongly typed generated methods.

Illustrative API:

```csharp
Func<object, object> CreateObjectAdapter<TSource, TDestination>(
    object instance,
    string methodName);

Func<object, object, object> CreateObjectAdapter<TSource, TContext, TDestination>(
    object instance,
    string methodName);
```

The exact API shape can be different. The goal is to let Dynabee own efficient object-to-typed adapters instead of OctoMap using reflection to build generic helper delegates.

### Why this is generic

Many runtime systems need a fast object-based surface over typed generated methods. Examples include generic serialization, validation, routing, message dispatch, event handling, and dynamic adapters.

### OctoMap impact

OctoMap can improve `Map<TDestination>(object source)` and dynamic fallback paths while keeping the generated mapper body strongly typed.

## Required feature 7: method identity metadata

Dynabee should expose stable metadata for generated methods that can be used as cache keys.

Illustrative shape:

```csharp
public sealed class DynaBeeGeneratedMethodDescriptor
{
    public Type DeclaringType { get; }
    public string Name { get; }
    public IReadOnlyList<Type> ParameterTypes { get; }
    public Type ReturnType { get; }
}
```

This descriptor could be returned when methods are added, or exposed from the assembly context after build.

### Why this is generic

Generated-method descriptors help diagnostics, invoker caching, generated delegate creation, analyzers, tracing, and debugging for any Dynabee consumer.

### OctoMap impact

OctoMap can avoid rebuilding method lookup inputs and can keep backend-neutral compiled map metadata cleaner.

## Non-goals

Dynabee should not implement:

- AutoMapper-like mapping configuration
- OctoMap profiles
- OctoMap-specific resolver or converter behavior
- dependency injection lifetime policies
- projection expression building
- mapping validation
- mapping diagnostics
- EF projection support

Those belong in OctoMap or in other consumers.

## Acceptance scenarios

Dynabee is ready for the next OctoMap performance pass when these scenarios work through public Dynabee APIs only.

### Scenario 1: bound typed delegate

Dynabee can generate:

```csharp
public sealed class GeneratedAdder
{
    public int Add(int left, int right) => left + right;
}
```

And return:

```csharp
Func<int, int, int> add = context.CreateBoundDelegate<Func<int, int, int>>(
    "GeneratedAdder",
    instance,
    "Add",
    new[] { typeof(int), typeof(int) });
```

Calling `add(1, 2)` returns `3` without `MethodInfo.Invoke`.

### Scenario 2: overloaded generated methods

Dynabee can generate:

```csharp
public sealed class GeneratedFormatter
{
    public string Format(int value) => value.ToString();
    public string Format(decimal value) => value.ToString("0.00");
}
```

And create delegates for each overload by parameter types.

### Scenario 3: typed factory delegate

Dynabee can generate a class with constructor parameters and return a typed factory delegate that creates instances without consumer-side reflection.

### Scenario 4: high-arity bound delegate

Dynabee can generate a method with at least 10 input parameters plus a context parameter and return a matching typed delegate.

### Scenario 5: object adapter

Dynabee can create a fast object-based adapter around a typed generated method:

```csharp
Func<object, object> adapter = context.CreateObjectAdapter(...);
```

The adapter performs casts and calls the generated method without reflective invocation.

## Proposed implementation order

1. Add overload-safe method registration and lookup.
2. Add bound typed delegate creation.
3. Add open typed delegate creation.
4. Add typed factory delegate creation.
5. Add optional object adapter helpers.
6. Expose generated method descriptors.
7. Add coverage for high-arity delegates.
8. Publish a new Dynabee package.

## OctoMap changes after Dynabee update

After Dynabee ships these features, OctoMap can:

1. Replace compile-time `MakeGenericMethod(...).Invoke(...)` delegate bridge creation with Dynabee typed delegates.
2. Replace dynamic `object[]` fallback paths where possible with generated object adapters.
3. Simplify generated method names by using real overloads.
4. Add typed delegate caches for multi-source maps from 3 to 10 sources.
5. Reduce cold compile overhead.
6. Keep the backend boundary clean: OctoMap owns mapping semantics, Dynabee owns generated dispatch.

## Summary

OctoMap's generated mapper body is now fast enough that remaining performance work is mostly dispatch infrastructure.

The next useful Dynabee pass is a generic runtime-generation performance upgrade:

- strongly typed bound delegates
- open delegates
- overload-safe method lookup
- generated instance factories
- high-arity delegate support
- optional object adapters
- generated method descriptors

These features keep Dynabee broadly useful while giving OctoMap the tools to get closer to hand-written mapper performance without leaking reflection or IL concerns into OctoMap.
