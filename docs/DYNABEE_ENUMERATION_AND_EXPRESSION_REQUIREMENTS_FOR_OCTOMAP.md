# Dynabee enumeration and expression body requirements for OctoMap

## Status

Implemented by Dynabee `1.2.4` and integrated by OctoMap for direct `IEnumerable<T>` to list/interface collection mapping plus richer generated `MapFrom(...)` expressions.

This document was originally requested after reviewing Dynabee `1.2.3`. It now acts as the implementation record for the generic Dynabee capabilities OctoMap needed next.

Dynabee `1.2.3` already provides the body primitives OctoMap needed for array and `List<T>` collection mapping:

- runtime-sized array creation
- constructor calls with arguments
- indexed access and indexed assignment
- ordered comparisons
- `for` loops
- method calls and discarded-value statements

Dynabee `1.2.4` added the smaller, more specific set of generic generation features OctoMap needed next. OctoMap now uses those APIs without leaking IL, reflection invocation, or OctoMap-specific behavior into Dynabee.

## Purpose

OctoMap wants to keep this boundary:

```text
OctoMap owns mapping semantics and planning.
Dynabee owns generated type creation and method body emission.
```

The requested features are intentionally generic. They are useful for serializers, validators, adapters, import/export pipelines, projection builders, proxy generators, and any library that needs to generate complete methods with collection enumeration or non-trivial value expressions.

## Previous OctoMap behavior

OctoMap can currently generate mapper methods through Dynabee for:

- convention-based property assignment
- configured member assignment
- null substitution
- DI value resolvers
- DI value converters
- nested object mapping
- array member mapping
- `List<T>` member mapping
- interface collection destinations backed by `List<T>`
- multi-source object mapping

Before Dynabee `1.2.4`, collection sources declared as `IEnumerable<T>` or similar non-indexed shapes were normalized with:

```csharp
Enumerable.ToList<T>(sourceCollection)
```

Then OctoMap emitted a generated `for` loop over the temporary list.

That worked, but it forced eager materialization and an extra allocation before mapping started. OctoMap now uses Dynabee `ForEach(...)` for enumerable sources when the destination can be represented as a list-backed collection. Enumerable source mapping to arrays still requires materialization because the destination array length must be known before array creation.

Before Dynabee `1.2.4`, OctoMap also supported only a small expression subset in generated `MapFrom(...)` bodies:

- member access
- constants
- conversions
- conditional expressions
- `Add`
- `Equal`
- `NotEqual`
- limited multi-source context access

OctoMap now uses Dynabee body primitives for arithmetic, comparisons, short-circuiting boolean expressions, boolean negation, and coalesce. Method-backed operators such as `decimal` arithmetic are generated through Dynabee static method calls.

## Required feature: `ForEach` statement support

Dynabee should expose a high-level `foreach` body primitive over enumerable values.

Suggested API shape:

```csharp
body.ForEach(sourceItems, (item, loop) =>
{
    var mappedItem = loop.Call(mapper, mapMethod, item);
    loop.Evaluate(loop.Call(destinationItems, addMethod, mappedItem));
});
```

Alternative API shapes are fine. The important part is that callers can enumerate a runtime value through Dynabee's public body API without emitting raw IL and without forcing `Enumerable.ToList(...)`.

### Required source shapes

Minimum useful source shapes:

- arrays
- `List<T>`
- `IEnumerable<T>`
- `IReadOnlyCollection<T>`
- `IReadOnlyList<T>`
- `ICollection<T>`
- `IList<T>`

Dynabee may optimize arrays and lists internally with indexed loops, but callers should not need to know that.

### Enumerator disposal

For general `IEnumerable<T>`, Dynabee should preserve normal C# `foreach` disposal semantics:

```csharp
var enumerator = source.GetEnumerator();
try
{
    while (enumerator.MoveNext())
    {
        var item = enumerator.Current;
        ...
    }
}
finally
{
    enumerator?.Dispose();
}
```

Dynabee can either:

- hide all of this inside `ForEach(...)`, or
- expose reusable lower-level primitives for `try/finally` and loop control.

OctoMap prefers the first option because the generated mapping plan only cares about enumeration, not enumerator mechanics.

### Acceptance criteria

Dynabee should be able to generate the following through public body APIs only.

```csharp
public List<string> Copy(IEnumerable<string> source)
{
    if (source == null)
    {
        return null;
    }

    var destination = new List<string>();

    foreach (var item in source)
    {
        destination.Add(item);
    }

    return destination;
}
```

```csharp
public List<OrderItemDto> MapItems(IEnumerable<OrderItem> source, IItemMapper mapper)
{
    if (source == null)
    {
        return null;
    }

    var destination = new List<OrderItemDto>();

    foreach (var item in source)
    {
        destination.Add(mapper.Map(item));
    }

    return destination;
}
```

```csharp
public OrderDto Map(Order source, IItemMapper mapper)
{
    var destination = new OrderDto();
    destination.Items = new List<OrderItemDto>();

    foreach (var item in source.Items)
    {
        destination.Items.Add(mapper.Map(item));
    }

    return destination;
}
```

## Optional supporting feature: `TryFinally`

If Dynabee does not hide enumerator disposal inside `ForEach(...)`, it should expose structured `try/finally` support.

Suggested API shape:

```csharp
body.Try(
    tryBody =>
    {
        // emitted statements
    },
    finallyBody =>
    {
        // disposal or cleanup
    });
```

This should be generic statement-level functionality. It should not be tied to enumerators or collections.

OctoMap does not require `catch` support for this pass.

## Optional supporting feature: loop control

If Dynabee exposes lower-level enumeration primitives instead of a complete `ForEach(...)`, it may also need:

```csharp
body.Break();
body.Continue();
```

OctoMap does not need `break` or `continue` for basic collection mapping. These are useful only if Dynabee chooses a composable lower-level API.

## Required feature: boolean and logical body expressions

OctoMap needs to support richer generated `MapFrom(...)` expressions without switching to expression compilation or handwritten helpers.

Dynabee should expose these operations on `IBeeMethodBodyBuilder`:

```csharp
body.AndAlso(left, right);
body.OrElse(left, right);
body.Not(value);
```

Required semantics:

- `AndAlso` must short-circuit.
- `OrElse` must short-circuit.
- `Not` should support boolean operands.
- validation should fail early for unsupported operand types.

### Acceptance criteria

Dynabee should generate:

```csharp
public bool ShouldMap(Source source)
{
    return source.IsActive && source.Count > 0;
}
```

```csharp
public bool ShouldSkip(Source source)
{
    return !source.IsActive || source.DeletedAt != null;
}
```

## Required feature: coalesce expression

OctoMap needs null fallback expressions beyond the explicit `NullSubstitute(...)` configuration API.

Suggested API shape:

```csharp
body.Coalesce(value, fallback);
```

Required semantics:

- reference type coalescing
- nullable value type coalescing
- result type should follow C#-compatible assignability rules where practical
- validation should fail early for incompatible operands

### Acceptance criteria

Dynabee should generate:

```csharp
public string GetName(Source source)
{
    return source.DisplayName ?? source.Name ?? "Unknown";
}
```

```csharp
public decimal GetTotal(Source source)
{
    return source.TotalOverride ?? source.Total;
}
```

## Required feature: arithmetic body expressions

Dynabee already exposes `Add(...)`. OctoMap needs the rest of the common arithmetic operations for value mapping expressions.

Requested API:

```csharp
body.Subtract(left, right);
body.Multiply(left, right);
body.Divide(left, right);
body.Modulo(left, right);
body.Negate(value);
```

Required support:

- common primitive numeric types
- nullable variants can be deferred if the validation message is clear
- checked arithmetic is not required for this pass

### Acceptance criteria

Dynabee should generate:

```csharp
public decimal GetLineTotal(LineItem source)
{
    return source.UnitPrice * source.Quantity;
}
```

```csharp
public decimal GetDiscountedTotal(Order source)
{
    return source.Subtotal - source.Discount;
}
```

```csharp
public int GetPageOffset(PageRequest source)
{
    return (source.Page - 1) * source.PageSize;
}
```

## Optional feature: null-safe member chain helper

OctoMap can already build null checks manually with Dynabee's current `If(...)` and `IsNull(...)` primitives, so this is not a hard blocker.

A generic helper would still improve generated body construction for nested object access:

```csharp
body.NullSafe(
    root: source,
    access: value => value.Property("Customer").Property("Address").Property("City"),
    fallback: body.Default(typeof(string)));
```

The API shape above is only illustrative. This should be considered optional unless other Dynabee consumers need it too.

## What Dynabee does not need to add

Dynabee does not need OctoMap-specific APIs such as:

- `CreateMap`
- `MapFrom`
- resolver concepts
- converter concepts
- source/destination mapping plans
- collection mapping policies

Dynabee also does not need to provide:

- LINQ translation
- query provider projection support
- async mapper generation
- expression compilation wrappers
- reflection invocation helpers
- AutoMapper-compatible APIs

OctoMap can own all of that above Dynabee.

## Proposed implementation order

1. Add `ForEach(...)` with correct enumerable semantics.
2. Add tests for direct `IEnumerable<T>` to `List<T>` copy and transform scenarios.
3. Add `AndAlso`, `OrElse`, and `Not` to the body builder.
4. Add `Coalesce(...)`.
5. Add arithmetic operations: `Subtract`, `Multiply`, `Divide`, `Modulo`, and `Negate`.
6. Publish a new Dynabee package.
7. Return to OctoMap and remove the `Enumerable.ToList(...)` normalization workaround for enumerable collection sources.
8. Expand OctoMap's generated `MapFrom(...)` expression support using the new body primitives.

## OctoMap changes after Dynabee update

Once these features exist, OctoMap can make these improvements:

- map `IEnumerable<TSource>` directly into destination collections without forced `ToList()`
- preserve lazy source enumeration behavior where possible
- reduce allocation for enumerable mapping
- support richer `MapFrom(...)` expressions
- keep generated mapper bodies fully owned by Dynabee
- keep runtime mapping free from reflection invocation and expression compilation

## Summary

Dynabee `1.2.3` unblocked generated array and `List<T>` collection mapping.

The next useful Dynabee pass is not OctoMap-specific. It is a generic method-body generation upgrade:

- `ForEach(...)` over enumerable values
- correct enumerator disposal
- short-circuit boolean operators
- coalesce
- common arithmetic operators

With those features, OctoMap can remove its enumerable normalization workaround and support a much more natural set of generated `MapFrom(...)` expressions while preserving the architecture:

```text
OctoMap describes what to map.
Dynabee generates how the method executes.
```
