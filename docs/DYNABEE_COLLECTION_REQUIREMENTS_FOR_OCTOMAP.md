# Dynabee method body requirements for collection mapping

## Status

Implemented by Dynabee `1.2.3` and integrated by OctoMap for array and `List<T>` member mapping.

OctoMap reviewed the current Dynabee method body API before implementing collection mapping. Dynabee already supports enough generic body-building primitives for object mapping, nested mapping, DI resolvers, and value converters, but collection mapping requires additional statement-level control flow and collection/array primitives.

## Purpose

OctoMap needs to generate complete mapper methods for collection members without falling back to handwritten helpers, raw IL, reflection, or expression compilation in the hot path.

The desired generated method shape is:

```csharp
public OrderDto Map(Order source, IMapContext context)
{
    if (source == null)
    {
        return null;
    }

    var destination = new OrderDto();

    if (source.Items == null)
    {
        destination.Items = null;
    }
    else
    {
        var items = new List<OrderItemDto>(source.Items.Count);

        foreach (var item in source.Items)
        {
            items.Add(context.Services
                .GetRequiredService<IOctoMapper>()
                .Map<OrderItem, OrderItemDto>(item));
        }

        destination.Items = items;
    }

    return destination;
}
```

This document is intentionally framed as generic Dynabee capability. The requested features are useful beyond OctoMap for serializers, adapters, DTO generators, validators, import/export pipelines, proxies, and any generated type that needs to build or transform collections.

## Current Dynabee capability

Dynabee currently exposes a public method body builder with these useful primitives:

- method parameters
- local variables
- parameterless object construction
- property and field access
- constants
- default values
- conversions
- equality and inequality checks
- null checks
- addition and string concatenation
- conditional value expressions
- conditional statement blocks
- assignment
- instance method calls
- static method calls
- `Self()`
- `Evaluate(...)`
- `Return(...)`

These are enough for OctoMap scenarios such as:

- convention property assignment
- `MapFrom(...)` expressions within the supported expression subset
- `NullSubstitute(...)`
- value converters resolved from DI
- value resolvers resolved from DI
- nested object mapping through `IOctoMapper`
- multi-source object mapping

## Current gap

Collection mapping requires generated method bodies to express loops and indexed access. The current body builder does not expose these primitives:

- `for`
- `foreach`
- `while`
- less-than / greater-than comparisons
- indexed array/list access
- indexed assignment
- runtime-sized array creation
- constructor calls with arguments

Without those features, OctoMap would have to choose one of these weaker alternatives:

- call a handwritten collection helper from generated code
- compile expression/delegate helpers separately
- emit raw IL directly
- use reflection or late-bound calls in the hot path

Those alternatives work against the OctoMap/Dynabee design boundary: OctoMap should describe the mapping plan, and Dynabee should own method body generation.

## Required feature: loop statements

Dynabee should expose statement-level loop support.

Suggested API shape:

```csharp
body.For(
    initialize: loop => loop.Assign(index, loop.Constant(0)),
    condition: loop => loop.LessThan(index, count),
    increment: loop => loop.Assign(index, loop.Add(index, loop.Constant(1))),
    body: loop =>
    {
        // loop body
    });
```

Alternative shapes are fine. The important requirement is that callers can generate a loop without touching `ILGenerator`.

### Required scenarios

Dynabee should support generating:

```csharp
for (var i = 0; i < source.Length; i++)
{
    destination[i] = source[i];
}
```

and:

```csharp
for (var i = 0; i < source.Count; i++)
{
    destination.Add(Convert(source[i]));
}
```

## Required feature: foreach statements

`foreach` support would make collection mapping much cleaner for `IEnumerable<T>`.

Suggested API shape:

```csharp
body.ForEach(sourceItems, (item, loop) =>
{
    loop.Evaluate(loop.Call(destinationItems, addMethod, mappedItem));
});
```

Dynabee can implement this internally using the standard enumerator pattern:

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
    (enumerator as IDisposable)?.Dispose();
}
```

If `try/finally` is too much for the first implementation, a `for` loop over arrays and `IList<T>` is enough to unblock OctoMap's first collection pass.

## Required feature: comparison expressions

Dynabee currently supports equality-oriented checks. Collection loops need ordered comparisons.

Required expressions:

```csharp
body.LessThan(left, right);
body.LessThanOrEqual(left, right);
body.GreaterThan(left, right);
body.GreaterThanOrEqual(left, right);
```

These should support common primitive numeric types used for loop counters:

- `int`
- `long`
- nullable variants are not required initially

Validation should fail early for unsupported comparison operands.

## Required feature: indexed access

Dynabee should expose index expressions for arrays and indexer properties.

Suggested API:

```csharp
var item = body.Index(sourceArray, index);
body.Assign(body.Index(destinationArray, index), mappedItem);
```

Required support:

- array read: `source[i]`
- array write: `destination[i] = value`
- `List<T>` read through indexer: `source[i]`
- `IList<T>` read through indexer: `source[i]`

Optional but useful:

- `List<T>` write through indexer
- `IList<T>` write through indexer
- custom indexer properties

The index expression should implement `IBeeAssignableExpression` when the target supports assignment.

## Required feature: runtime-sized array creation

OctoMap needs to generate array destinations:

```csharp
var destination = new OrderItemDto[source.Length];
```

Suggested API:

```csharp
var destination = body.NewArray(typeof(OrderItemDto), lengthExpression);
```

Minimum requirements:

- one-dimensional zero-based arrays
- runtime length expression
- element type supplied as `Type`
- return type should be the array type, e.g. `OrderItemDto[]`

Multi-dimensional arrays are not required.

## Required feature: constructor calls with arguments

The current `body.New(type)` supports parameterless construction. Collection mapping benefits from constructors with arguments:

```csharp
var destination = new List<OrderItemDto>(source.Count);
```

Suggested API:

```csharp
body.New(typeof(List<OrderItemDto>), countExpression);
```

Required support:

- constructor overload resolution by argument expression types
- closed generic constructed types, e.g. `typeof(List<>).MakeGenericType(itemType)`
- useful validation if no constructor matches

This is generic functionality and useful for many generated object construction scenarios, not only collections.

## Required feature: statement-friendly method calls

Dynabee already has `Evaluate(...)`, which can discard return values. This should continue working for collection methods such as:

```csharp
destinationItems.Add(mappedItem);
```

Example:

```csharp
body.Evaluate(body.Call(destinationItems, addMethod, mappedItem));
```

No new API is required if `Evaluate(...)` already supports void and non-void calls reliably.

## Optional feature: collection helpers in Dynabee

Dynabee does not need OctoMap-specific collection APIs. However, generic convenience helpers could be useful:

```csharp
body.NewList(elementType);
body.NewList(elementType, capacityExpression);
body.CallAdd(listExpression, itemExpression);
body.Count(collectionExpression);
body.Length(arrayExpression);
```

These are optional. OctoMap can build the same behavior from lower-level primitives if loops, indexers, array creation, and constructor arguments are available.

## OctoMap first collection targets

Once Dynabee supports the required primitives, OctoMap's first collection pass can support:

### List to List

```csharp
public List<OrderItemDto> Items { get; set; }
```

from:

```csharp
public List<OrderItem> Items { get; set; }
```

Generated shape:

```csharp
var destinationItems = new List<OrderItemDto>(source.Items.Count);

for (var i = 0; i < source.Items.Count; i++)
{
    destinationItems.Add(mapper.Map<OrderItem, OrderItemDto>(source.Items[i]));
}

destination.Items = destinationItems;
```

### Array to Array

```csharp
public OrderItemDto[] Items { get; set; }
```

from:

```csharp
public OrderItem[] Items { get; set; }
```

Generated shape:

```csharp
var destinationItems = new OrderItemDto[source.Items.Length];

for (var i = 0; i < source.Items.Length; i++)
{
    destinationItems[i] = mapper.Map<OrderItem, OrderItemDto>(source.Items[i]);
}

destination.Items = destinationItems;
```

### Assignable item types

If the source item type is already assignable to the destination item type, OctoMap can copy item references or values without nested mapping:

```csharp
List<string> -> List<string>
int[] -> int[]
```

Generated code still needs loops unless OctoMap intentionally calls framework helpers such as `ToList()` or `Array.Copy(...)`.

## Null handling target

The initial OctoMap behavior should be:

```csharp
destination.Items = source.Items == null ? null : mappedItems;
```

This requires only existing Dynabee null checks and conditionals once the mapped collection expression can be generated.

Later OctoMap may add options such as:

```csharp
AllowNullCollections = false
```

which would generate:

```csharp
destination.Items = source.Items == null
    ? new List<OrderItemDto>()
    : mappedItems;
```

No special Dynabee support is needed beyond collection construction.

## Performance requirements

Generated collection mapping should avoid these in the hot path:

- reflection invocation
- `MethodInfo.Invoke(...)`
- per-item expression compilation
- per-item delegate lookup
- per-item dictionary lookup
- boxing loop counters or value-type items where avoidable

Reflection during generation is acceptable.

Generated code may call another generated mapper for item conversion:

```csharp
mapper.Map<OrderItem, OrderItemDto>(item)
```

That call should use OctoMap's existing generated mapper cache.

## Error behavior

Dynabee should fail early with clear exceptions when:

- a loop condition is not boolean
- a loop variable is not assignable
- an index expression is used on a non-indexable type
- an index expression uses an incompatible index type
- array length expression is not an integer-compatible type
- no constructor overload matches `body.New(type, args...)`
- a comparison operand pair is unsupported
- an indexed assignment target is read-only

Error messages should identify the generated type and method when possible.

## Acceptance criteria

Dynabee is ready for OctoMap collection mapping when these scenarios can be generated through public Dynabee APIs only.

### For loop over array

```csharp
public int[] Copy(int[] source)
{
    if (source == null)
    {
        return null;
    }

    var destination = new int[source.Length];

    for (var i = 0; i < source.Length; i++)
    {
        destination[i] = source[i];
    }

    return destination;
}
```

### For loop over list

```csharp
public List<string> Copy(List<string> source)
{
    if (source == null)
    {
        return null;
    }

    var destination = new List<string>(source.Count);

    for (var i = 0; i < source.Count; i++)
    {
        destination.Add(source[i]);
    }

    return destination;
}
```

### List transformation with method call

```csharp
public List<OrderItemDto> MapItems(List<OrderItem> source, IItemMapper mapper)
{
    if (source == null)
    {
        return null;
    }

    var destination = new List<OrderItemDto>(source.Count);

    for (var i = 0; i < source.Count; i++)
    {
        destination.Add(mapper.Map(source[i]));
    }

    return destination;
}
```

### Collection member assignment

```csharp
public OrderDto Map(Order source)
{
    var destination = new OrderDto();
    destination.Items = MapItems(source.Items);
    return destination;
}
```

OctoMap does not require Dynabee to provide `MapItems(...)`; this acceptance case only proves collection values can be assigned after generation.

## Proposed implementation order in Dynabee

1. Add comparison expressions: `LessThan`, `LessThanOrEqual`, `GreaterThan`, `GreaterThanOrEqual`.
2. Add constructor calls with arguments: `New(Type, params IBeeValueExpression[])`.
3. Add indexed access for arrays and indexer properties.
4. Add runtime-sized one-dimensional array creation.
5. Add `For(...)` statement support.
6. Add tests for array copy and `List<T>` copy.
7. Optionally add `ForEach(...)` after `For(...)` is stable.
8. Publish a new Dynabee version.
9. Return to OctoMap and implement generated collection mapping.

## Non-goals for the first Dynabee pass

These are valuable later but not required to unblock OctoMap collections:

- LINQ translation
- async loops
- iterator blocks
- `yield return`
- `try/catch`
- full `try/finally` foreach disposal semantics
- multi-dimensional arrays
- spans / memory types
- collection initializer syntax
- collection-specific OctoMap APIs inside Dynabee

## Summary

OctoMap can already generate object mapping, nested mapping, resolvers, converters, and multi-source mapping through Dynabee's public method body builder.

The next blocker is collection mapping. To keep the architecture clean, Dynabee should add generic method body primitives for loops, comparisons, indexed access, runtime-sized arrays, and constructor calls with arguments.

Once these exist, OctoMap can generate collection mapping directly through Dynabee, preserving the boundary:

```text
OctoMap plans mapping semantics.
Dynabee generates executable method bodies.
```
