# Dynabee runtime dispatch requirements

## Status

Dynabee `1.2.5` added the first generic runtime dispatch primitives:

- typed bound delegates through `CreateBoundDelegate<TDelegate>(...)`
- typed open delegates through `CreateOpenDelegate<TDelegate>(...)`
- typed factory delegates through `CreateFactoryDelegate<TDelegate>(...)`
- generated method descriptors
- single-argument object adapters through `CreateObjectAdapter(...)`

Dynabee `1.2.6` added the follow-up dispatch primitives requested here:

- runtime delegate-type overloads through `CreateBoundDelegate(Type, ...)`
- runtime factory delegate-type overloads through `CreateFactoryDelegate(Type, ...)`
- two-argument object adapters through `CreateObjectAdapter2(...)`
- three-argument object adapters through `CreateObjectAdapter3(...)`
- argument-list object adapters through `CreateArgumentListAdapter(...)`
- a generic `IDynaBeeObjectMethodAdapter`

OctoMap has integrated the bound delegate API, the single-argument object adapter API, the two-argument object adapter API, and the argument-list adapter API. The remaining asks below are not mapping features. They are generic runtime-generation capabilities that would help any library needing a fast object-based dispatch layer over strongly typed generated methods.

## Design intent

Dynabee should stay a general-purpose runtime generation engine.

Dynabee should own:

- runtime type generation
- generated method body emission
- generated instance creation
- generated method lookup
- generated method delegate binding
- generated object-to-typed adapter dispatch

Dynabee should not own:

- object mapping configuration
- AutoMapper-like profiles
- OctoMap-specific source or destination concepts
- resolver or converter semantics
- dependency injection lifetime policy
- projection expression building
- validation rules from any consumer library

The useful boundary is:

```text
consumer library owns semantics
Dynabee owns generated execution infrastructure
```

## Current remaining gap

Typed generated methods are fast when the caller knows the method signature at compile time:

```csharp
Func<TInput, TContext, TResult> compiled =
    context.CreateBoundDelegate<Func<TInput, TContext, TResult>>(
        className,
        instance,
        methodName,
        new[] { typeof(TInput), typeof(TContext) });
```

The remaining overhead appears when a consumer exposes an object-based API over generated typed methods. That shape is common in serializers, validators, routers, message dispatchers, object mappers, import/export pipelines, and plugin systems.

Today a consumer must either:

- allocate an `object[]` and call a generic invoker
- write its own reflection or expression-tree adapter
- close helper methods with `MakeGenericMethod(...).Invoke(...)`
- create one-off typed wrappers outside Dynabee

Those options work, but they duplicate dispatch infrastructure that fits naturally inside Dynabee.

## Request 1: multi-argument object adapters

Status: implemented by Dynabee `1.2.6` for two-argument, three-argument, and argument-list object adapters. OctoMap uses `CreateObjectAdapter2(...)` for context-aware object dispatch.

Dynabee should support generated object adapters for methods with more than one argument.

The API shape can vary. The important part is that Dynabee creates and owns the adapter that casts object inputs, calls the strongly typed generated method, and boxes the result only when needed.

Illustrative API:

```csharp
public interface IDynaBeeAssemblyContext
{
    Func<object, object> CreateObjectAdapter(
        string className,
        object instance,
        string methodName,
        IReadOnlyList<Type> parameterTypes);

    Func<object, object, object> CreateObjectAdapter2(
        string className,
        object instance,
        string methodName,
        IReadOnlyList<Type> parameterTypes);

    Func<IReadOnlyList<object>, object> CreateArgumentListAdapter(
        string className,
        object instance,
        string methodName,
        IReadOnlyList<Type> parameterTypes);
}
```

Alternative naming is fine. A params-style API is also fine if it avoids per-call array allocation in fixed-arity cases.

### Required behavior

- Select the generated method by method name and exact parameter types.
- Cast or unbox each object argument to the generated method parameter type.
- Call the generated method without `MethodInfo.Invoke`.
- Return the generated method result as `object`.
- Support `void` methods by returning `null` or a documented sentinel.
- Fail early when an argument cannot be cast to the required parameter type.
- Fail early when the method cannot be found or is ambiguous.

### Why this is generic

This is useful for any runtime-generated method that must be exposed through a dynamic dispatch surface:

- serializer property readers/writers
- validator rules
- message handlers
- route handlers
- plugin commands
- generated import/export transforms
- generated state transitions
- generated object mappers

## Request 2: fixed-arity object adapters up to common delegate limits

Status: partially implemented by Dynabee `1.2.6` with `CreateObjectAdapter2(...)` and `CreateObjectAdapter3(...)`. Higher fixed arities can be added later if benchmarks justify avoiding the argument-list adapter.

For hot paths, Dynabee should expose fixed-arity object adapters so consumers do not need to allocate an `object[]` per call.

Useful shapes:

```csharp
Func<object, object>                    // 1 argument
Func<object, object, object>            // 2 arguments
Func<object, object, object, object>    // 3 arguments
```

For higher arity, Dynabee can expose named delegate types or a generic adapter abstraction.

Illustrative abstraction:

```csharp
public interface IDynaBeeObjectMethodAdapter
{
    IReadOnlyList<Type> ParameterTypes { get; }

    Type ReturnType { get; }

    object Invoke(IReadOnlyList<object> arguments);
}

public interface IDynaBeeObjectMethodAdapter<T1, TResult>
{
    TResult Invoke(T1 arg1);
}

public interface IDynaBeeObjectMethodAdapter<T1, T2, TResult>
{
    TResult Invoke(T1 arg1, T2 arg2);
}
```

The exact model is flexible. The key requirement is to let consumers avoid building their own object dispatch adapters around generated methods.

### Required behavior

- Support at least 10 generated method parameters for fixed or typed adapter creation.
- Keep overload resolution based on explicit parameter types.
- Avoid per-call `object[]` allocations in fixed-arity adapters.
- Avoid reflective invocation.
- Cache or reuse the generated dispatch plan when possible.

### Why this is generic

Generated code frequently receives multiple runtime inputs: input model, context, destination object, cancellation token, metadata, service scope, route data, or message envelope. Multi-argument object adapters are a runtime dispatch primitive, not a mapper feature.

## Request 3: typed adapter creation from runtime type arrays

Status: implemented by Dynabee `1.2.6` as `CreateBoundDelegate(Type, ...)`.

Dynabee already creates typed delegates when the caller supplies `TDelegate`. Some consumers discover source and destination types only at runtime. They still need Dynabee to own the fast adapter creation.

Suggested capability:

```csharp
public interface IDynaBeeAssemblyContext
{
    object CreateBoundDelegate(
        Type delegateType,
        string className,
        object instance,
        string methodName,
        IReadOnlyList<Type> parameterTypes);
}
```

This would complement:

```csharp
TDelegate CreateBoundDelegate<TDelegate>(...);
```

### Required behavior

- Validate that `delegateType` is a delegate.
- Validate delegate parameter and return types against the selected method.
- Return a delegate instance assignable to `delegateType`.
- Avoid requiring consumers to call `MakeGenericMethod(...).Invoke(...)` just to reach Dynabee's typed delegate creation.

### Why this is generic

Runtime-generated frameworks often build dispatch from metadata discovered at runtime. This includes serializers, RPC clients, endpoint routers, validators, plugin hosts, materializers, and object mappers.

## Request 4: constructor factory creation from runtime delegate type

Status: implemented by Dynabee `1.2.6` as `CreateFactoryDelegate(Type, ...)`.

Dynabee already exposes typed factory delegates. The same runtime delegate-type overload would make factory creation easier for dynamic consumers.

Suggested capability:

```csharp
public interface IDynaBeeAssemblyContext
{
    object CreateFactoryDelegate(
        Type delegateType,
        string className,
        IReadOnlyList<Type> constructorParameterTypes);
}
```

### Required behavior

- Select the constructor by exact parameter types.
- Validate delegate parameter types against the constructor parameter types.
- Validate delegate return type against the generated class type.
- Return a delegate instance assignable to `delegateType`.
- Avoid reflective constructor invocation.

### Why this is generic

Any generated component system may need to create generated instances from runtime metadata: proxies, adapters, DTO materializers, validators, handlers, generated controllers, and test doubles.

## Acceptance scenarios

### Scenario 1: two-argument object adapter

Dynabee generates:

```csharp
public sealed class GeneratedAdder
{
    public int Add(int left, int right) => left + right;
}
```

A consumer can create:

```csharp
Func<object, object, object> add = context.CreateObjectAdapter2(
    "GeneratedAdder",
    instance,
    "Add",
    new[] { typeof(int), typeof(int) });
```

Calling:

```csharp
add(1, 2)
```

returns boxed `3` without `MethodInfo.Invoke` and without allocating an argument array.

### Scenario 2: context-aware object adapter

Dynabee generates:

```csharp
public sealed class GeneratedRule
{
    public bool IsValid(Order order, ValidationContext context) => order.Total > context.MinimumTotal;
}
```

A consumer can create an object adapter for:

```csharp
Func<object, object, object>
```

that casts the first argument to `Order`, casts the second argument to `ValidationContext`, and returns boxed `bool`.

### Scenario 3: runtime delegate type

A consumer discovers at runtime that the generated method signature is:

```csharp
int Add(int left, int right)
```

It builds:

```csharp
var delegateType = typeof(Func<int, int, int>);
```

and calls:

```csharp
var add = (Func<int, int, int>)context.CreateBoundDelegate(
    delegateType,
    "GeneratedAdder",
    instance,
    "Add",
    new[] { typeof(int), typeof(int) });
```

The returned delegate calls the generated method without `MethodInfo.Invoke`.

### Scenario 4: high-arity generated method

Dynabee generates a method with 10 input parameters and returns either:

- a typed delegate
- a fixed-arity object adapter
- a documented high-arity adapter abstraction

The method invocation should not require per-call `object[]` allocation when using a fixed-arity adapter.

## Non-goals

Dynabee should not add any of these:

- mapping profiles
- source/destination mapping configuration
- AutoMapper-compatible APIs
- OctoMap-specific interfaces
- service resolver or converter concepts
- dependency injection lifetime decisions
- projection support for LINQ providers
- validation rules belonging to a consumer library

## Proposed implementation order

1. Add runtime delegate-type overloads for bound delegate creation. Implemented in Dynabee `1.2.6`.
2. Add runtime delegate-type overloads for factory delegate creation. Implemented in Dynabee `1.2.6`.
3. Add two-argument and three-argument object adapters. Implemented in Dynabee `1.2.6`.
4. Add a high-arity object adapter strategy up to at least 10 parameters. Covered by `CreateArgumentListAdapter(...)` in Dynabee `1.2.6`; fixed-arity adapters above three arguments remain optional.
5. Add tests proving there is no `MethodInfo.Invoke` in adapter invocation.
6. Add allocation benchmarks comparing fixed-arity adapters vs `object[]` invokers.
7. Publish a new Dynabee package.

## Consumer impact

For OctoMap specifically, Dynabee `1.2.6` now allows:

- dynamic `Map<TDestination>(object source)` paths with context to use Dynabee object adapters
- dynamic source-set invocation to use Dynabee argument-list adapters
- cleaner backend code while keeping mapping semantics outside Dynabee

For other Dynabee consumers, the same features provide a reusable dispatch layer over generated strongly typed methods.
