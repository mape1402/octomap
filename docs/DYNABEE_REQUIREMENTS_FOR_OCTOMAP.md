# Dynabee requirements for OctoMap

## Purpose

OctoMap needs Dynabee to generate complete mapper methods from an OctoMap mapping plan without forcing OctoMap to emit raw IL directly.

The target shape is a generated mapper type with one complete `Map` method per mapping plan:

```csharp
public sealed class GeneratedUserMapper : IOctoMapper<User, UserDto>
{
    public UserDto Map(User source)
    {
        var destination = new UserDto();

        destination.Id = source.Id;
        destination.Name = source.Name;
        destination.DisplayName = source.FirstName + " " + source.LastName;
        destination.Status = "Created";

        return destination;
    }
}
```

OctoMap should describe the mapping plan. Dynabee should provide the body-generation API needed to translate that plan into efficient IL.

## Current Dynabee capability

Dynabee `1.1.0` already provides the pieces OctoMap needs for type generation:

- assembly generation
- class generation
- interface implementation
- method generation
- constructor generation
- property generation
- `Emits(Action<ILGenerator>)`
- `EmitsExpression(...)`
- `EmitsLambda(...)`

This is enough to create generated mapper classes, but not enough to express a complete mapper body at the Dynabee API level.

## Current gap

OctoMap currently has to use Dynabee's low-level `Emits(Action<ILGenerator>)` escape hatch and manually emit the body of the mapper method.

That means OctoMap owns IL details such as:

- local variable declaration
- destination construction
- property getter calls
- property setter calls
- null checks
- constants
- conversions
- default values
- return instructions

This is not the desired long-term design. OctoMap should remain a mapping library. Dynabee should own generated method body construction.

## Why `EmitsExpression` is not enough

Dynabee's expression emitter currently supports value-style expressions such as:

- constants
- parameters
- conversions
- binary operations
- method calls
- member access
- constructor calls
- conditionals

That works for simple value expressions like:

```csharp
source => source.FirstName + " " + source.LastName
```

But a complete mapper method needs statement-style behavior:

```csharp
var destination = new Destination();
destination.Name = source.Name;
destination.Total = source.Amount;
return destination;
```

The missing pieces are:

- `BlockExpression`
- `ExpressionType.Assign`
- `MemberInitExpression`
- local variables
- property assignment
- statement sequencing
- explicit return

OctoMap does not need Dynabee to expose raw expression-tree support specifically, but it does need an equivalent body-building abstraction.

## Required Dynabee feature: method body builder

Dynabee should expose a high-level API for building method bodies. The API can be named differently, but it should support the concepts below.

Example desired usage:

```csharp
assembly
    .AddClass("GeneratedUserMapper", type =>
    {
        type.Implements(typeof(IOctoMapper<User, UserDto>));

        type.AddMethod("Map", method =>
        {
            method
                .Returns(typeof(UserDto))
                .AddParameter(typeof(User), "source")
                .EmitsBody(body =>
                {
                    var source = body.Parameter("source");
                    var destination = body.DeclareLocal(typeof(UserDto), "destination");

                    body.Assign(destination, body.New(typeof(UserDto)));

                    body.Assign(
                        body.Property(destination, "Id"),
                        body.Property(source, "Id"));

                    body.Assign(
                        body.Property(destination, "DisplayName"),
                        body.Add(
                            body.Add(body.Property(source, "FirstName"), body.Constant(" ")),
                            body.Property(source, "LastName")));

                    body.Assign(
                        body.Property(destination, "Status"),
                        body.Constant("Created"));

                    body.Return(destination);
                });
        });
    });
```

The important part is not the exact syntax. The important part is that OctoMap can express a full method body without touching `ILGenerator`.

## Minimum body operations

Dynabee should support these method body operations first.

### Parameters

Dynabee must allow retrieving method parameters by name or index:

```csharp
var source = body.Parameter("source");
```

### Locals

Dynabee must allow declaring local variables:

```csharp
var destination = body.DeclareLocal(typeof(UserDto), "destination");
```

### Object construction

Dynabee must allow constructing objects using a parameterless constructor:

```csharp
body.New(typeof(UserDto));
```

Later this should support constructor arguments:

```csharp
body.New(typeof(UserDto), arg1, arg2);
```

### Assignment

Dynabee must allow assigning values to:

- local variables
- writable properties
- writable fields, if supported

```csharp
body.Assign(destination, body.New(typeof(UserDto)));
body.Assign(body.Property(destination, "Name"), body.Property(source, "Name"));
```

### Property access

Dynabee must support property getters and setters:

```csharp
var sourceName = body.Property(source, "Name");
var destinationName = body.Property(destination, "Name");
```

The builder should validate property readability/writability before generation.

### Constants

Dynabee must support constants for common primitive and framework types:

- `null`
- `string`
- `bool`
- `char`
- signed integers
- unsigned integers
- floating-point numbers
- `decimal`
- `DateTime`
- `DateTimeOffset`
- `Guid`
- enums

For values that cannot be encoded directly as IL constants, Dynabee can use an internal constant table.

### Type conversion

Dynabee must support common assignment-compatible conversions:

- identity conversion
- implicit numeric conversion
- explicit numeric conversion when requested
- nullable wrapping
- nullable unwrapping with validation or fallback
- enum to underlying type
- underlying type to enum
- reference casts
- boxing
- unboxing

The API should allow OctoMap to choose whether a conversion is required or whether unsupported conversion should fail during mapper generation.

### Return

Dynabee must allow returning an expression:

```csharp
body.Return(destination);
```

### Null checks and conditionals

Dynabee must support simple conditional blocks or conditional expressions:

```csharp
body.If(
    body.NotEqual(body.Property(source, "Name"), body.Null(typeof(string))),
    thenBody => thenBody.Assign(
        body.Property(destination, "Name"),
        body.Property(source, "Name")),
    elseBody => elseBody.Assign(
        body.Property(destination, "Name"),
        body.Constant("Unknown")));
```

This is required for `NullSubstitute`, nullable handling, and future conditional mapping.

## Required expression operations

The body builder should reuse Dynabee's existing expression emitter where possible, but OctoMap also needs these expression-level operations:

- property access
- field access, optional but useful
- method calls
- constructor calls
- binary operations
- unary conversions
- conditional expressions
- null constants with explicit type
- default value expressions
- enum constants
- decimal constants
- nullable value access
- nullable `HasValue`

## Required validation behavior

Dynabee should fail early with useful exceptions when a body cannot be generated.

Examples:

- destination type has no usable constructor
- destination member is not writable
- source member is not readable
- conversion is unsupported
- assignment target is invalid
- expression type does not match method return type
- null is assigned to a non-nullable value type without an explicit fallback

The exception should identify the generated type/method when possible.

## Required performance characteristics

OctoMap is using Dynabee specifically to avoid reflection-heavy runtime mapping. The generated mapper method should:

- call property getters and setters directly
- avoid reflection during map execution
- avoid dictionary lookups during map execution
- avoid expression compilation during map execution
- avoid delegate invocation per destination property when possible
- avoid `object[]` argument passing in the hot path
- avoid boxing for value-type properties when possible

Reflection is acceptable during generation. It is not acceptable in the generated mapper hot path.

## Required dependency model

Dynabee can keep low-level IL internals internal. OctoMap should depend on public Dynabee abstractions only.

The preferred dependency boundary:

```csharp
OctoMap MappingPlan
    -> OctoMap Dynabee adapter
        -> Dynabee public body builder API
            -> Dynabee internal IL generation
```

OctoMap should not reference Dynabee internal types.

## Optional but highly valuable: MemberInit support

As an alternative or complement to a custom body builder, Dynabee could expand `EmitsExpression(...)` to support:

- `MemberInitExpression`
- `MemberAssignment`
- `BlockExpression`
- `ExpressionType.Assign`
- `DefaultExpression`

Then OctoMap could generate an expression tree similar to:

```csharp
Expression<Func<User, UserDto>> map =
    source => new UserDto
    {
        Id = source.Id,
        Name = source.Name
    };
```

However, a dedicated body builder is still preferable for OctoMap because future mapping scenarios will need richer statement-level control.

## Optional but important: reusable generated delegates

Dynabee could expose a way to generate a strongly typed delegate directly from a method body:

```csharp
Func<User, UserDto> map = DynabeeBody
    .Create<Func<User, UserDto>>()
    .Body(body => ...)
    .Build();
```

This is not required for the first OctoMap integration because OctoMap already expects generated mapper classes, but it could simplify internal cache paths later.

## Optional but important: AOT/mobile strategy

OctoMap wants Dynabee behind an adapter because runtime IL generation may not be available or desirable on every platform.

Dynabee should eventually expose capability detection:

```csharp
public interface IDynabeeRuntimeCapabilities
{
    bool SupportsRuntimeCodeGeneration { get; }
    bool SupportsDynamicAssemblySave { get; }
    bool SupportsExpressionEmission { get; }
}
```

This would let OctoMap select a backend:

- Dynabee runtime IL backend
- source-generator backend
- expression/delegate fallback backend
- interpreted fallback backend

This is not required before returning to OctoMap, but the API design should not block it.

## OctoMap phase unlocked by this Dynabee work

Once Dynabee exposes this body-generation API, OctoMap can replace most of `DynabeeMappingGenerationBackend` with a small translation layer:

```csharp
foreach (var assignment in plan.Assignments)
{
    body.Assign(
        body.Property(destination, assignment.DestinationMember),
        TranslateValueExpression(body, source, assignment));
}
```

OctoMap would keep responsibility for:

- reading profiles
- reading attributes
- reading `IMapFrom<T>` / `IMapTo<T>`
- implicit convention matching
- runtime map registration
- mapping plan creation
- plan validation
- generated mapper cache
- multi-source mapping semantics

Dynabee would own:

- generated type creation
- generated method creation
- method body API
- IL correctness
- IL optimization
- generated code diagnostics

## Acceptance criteria

Dynabee should be considered ready for the next OctoMap pass when these scenarios can be implemented without OctoMap touching `ILGenerator`.

### Simple convention mapper

Dynabee can generate:

```csharp
public UserDto Map(User source)
{
    var destination = new UserDto();
    destination.Id = source.Id;
    destination.Name = source.Name;
    return destination;
}
```

### Constant value mapper

Dynabee can generate:

```csharp
public UserDto Map(User source)
{
    var destination = new UserDto();
    destination.Status = "Created";
    return destination;
}
```

### Expression value mapper

Dynabee can generate:

```csharp
public UserDto Map(User source)
{
    var destination = new UserDto();
    destination.DisplayName = source.FirstName + " " + source.LastName;
    return destination;
}
```

### Null substitute mapper

Dynabee can generate:

```csharp
public UserDto Map(User source)
{
    var destination = new UserDto();
    destination.Name = source.Name == null ? "Unknown" : source.Name;
    return destination;
}
```

### Numeric conversion mapper

Dynabee can generate:

```csharp
public InvoiceDto Map(Invoice source)
{
    var destination = new InvoiceDto();
    destination.Total = (decimal)source.Total;
    return destination;
}
```

### Multiple source mapper

Dynabee can generate:

```csharp
public OrderDto Map(Order source, Customer customer)
{
    var destination = new OrderDto();
    destination.OrderId = source.Id;
    destination.CustomerName = customer.Name;
    return destination;
}
```

This is required for OctoMap's multiple-source mapping roadmap.

## Proposed implementation order in Dynabee

1. Add public body builder abstractions.
2. Implement parameters, locals, object construction, assignment, property access, and return.
3. Add constant handling with a constant table for non-IL-native constants.
4. Add conversion helpers.
5. Add conditionals and null checks.
6. Add tests for generated IL behavior.
7. Add XML documentation summaries for the new public API.
8. Publish a new Dynabee version.
9. Return to OctoMap and refactor `DynabeeMappingGenerationBackend` to use the new API.

## Suggested Dynabee API names

These names are suggestions only:

- `BeeMethodBuilder.EmitsBody(...)`
- `IBeeMethodBodyBuilder`
- `IBeeValueExpression`
- `IBeeAssignableExpression`
- `IBeeLocal`
- `IBeeParameter`
- `DeclareLocal(...)`
- `Parameter(...)`
- `New(...)`
- `Property(...)`
- `Field(...)`
- `Constant(...)`
- `Default(...)`
- `Convert(...)`
- `Assign(...)`
- `If(...)`
- `Return(...)`

The API should remain fluent, but it should not require OctoMap to know about IL opcodes.

## Non-goals for the first Dynabee pass

These are useful later, but not required to unblock OctoMap:

- loops
- try/catch/finally
- async methods
- iterator methods
- switch expressions/statements
- collection mapping helpers
- LINQ translation
- nested object graph mapping
- source generation
- persisted assembly output

## Summary

OctoMap needs Dynabee to grow from "type builder with IL escape hatch" into "type builder with method body builder".

The first required milestone is simple:

Dynabee must be able to generate a complete mapper method with local destination creation, property assignments, conditionals, conversions, and return, using public Dynabee APIs only.

Once that exists, OctoMap can remove direct mapper-body IL emission and use Dynabee as the true generation engine.
