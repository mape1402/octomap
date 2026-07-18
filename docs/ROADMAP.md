# OctoMap Roadmap

This document is the working roadmap for OctoMap. It captures the product direction, implementation priorities, architectural boundaries, and known feature gaps.

OctoMap's long-term goal is to provide an AutoMapper-style developer experience with generated runtime mapping performance, strong validation, projection support, and a clean backend boundary.

## Current Foundation

These capabilities are already implemented or have an initial working version:

- profile-based configuration
- dependency injection registration
- convention-based single-source mapping
- runtime implicit single-source maps with cache registration
- explicit multi-source mapping into one destination
- fluent member configuration
- interface-based map registration through `IMapFrom<T>` and `IMapTo<T>`
- DynaBee-backed generated mapper classes
- DynaBee-backed generated method invokers
- DynaBee-backed generated instance creation
- nested object mapping
- collection member mapping
- per-member and global null collection behavior
- DI value resolvers
- DI value converters
- generated `MapFrom(...)` expressions for common expression shapes
- method calls inside `MapFrom(...)`
- destination constructor mapping
- explicit `ConstructUsing(...)`
- record and immutable DTO construction by convention
- convention flattening for single-source maps
- basic `ReverseMap()`
- README, API docs, architecture docs, sample project, and unit tests

## Core Principles

- OctoMap owns mapping semantics, configuration, planning, validation, diagnostics, and public APIs.
- DynaBee owns runtime type generation, generated method body emission, generated instance creation, and generated invocation.
- DynaBee must stay behind an adapter boundary.
- Runtime mapping should not use reflection invocation as the hot path.
- Reflection during configuration, validation, and plan building is acceptable.
- Mapping behavior must be injectable, testable, and replaceable.
- Public abstractions should be interfaces; implementation classes should stay internal when possible.
- Static extension methods are allowed for registration and ergonomic API entry points.
- Static classes must not replace service abstractions or runtime components.
- Multi-source mapping must remain explicit to avoid ambiguity.
- Projection support must use expression trees, not DynaBee-generated IL, because LINQ providers such as Entity Framework need expression trees.

## Priority Roadmap

### 1. Projection Architecture

Projection support is essential for Entity Framework and other LINQ providers.

Target API:

```csharp
IQueryable<OrderDto> query = db.Orders.ProjectTo<OrderDto>(configuration);
```

or:

```csharp
IQueryable<OrderDto> query = db.Orders.ProjectTo<Order, OrderDto>(provider);
```

Required architecture:

- add `IProjectionExpressionBuilder`
- keep projection generation separate from `IMappingGenerationBackend`
- reuse OctoMap configuration and mapping plans where possible
- add validation for projectable maps
- expose clear errors for non-projectable features

Initial projection support:

- direct property mapping
- simple `MapFrom(...)` expressions
- flattening
- constructor projection for records and immutable DTOs
- null-safe nested member access where translatable
- nested object projection where provider-friendly

Initial projection exclusions:

- DI resolvers
- DI converters
- runtime service provider access
- `BeforeMap` / `AfterMap`
- complex method calls that EF cannot translate
- multi-source maps unless modeled explicitly as query joins later
- object reuse mapping

Future projection support:

- collection projection
- explicit expansion
- aggregate expressions such as `Count`, `Sum`, `Any`
- external projection parameters
- owned entities and complex types
- provider-specific capability validation

### 2. `ForPath(...)`

`ForPath(...)` enables explicit configuration of nested destination members.

Target API:

```csharp
builder.CreateMap<OrderDto, Order>()
    .ForPath(x => x.Customer.Name, x => x.MapFrom(s => s.CustomerName));
```

Required scenarios:

- explicit nested destination member assignment
- unflattening configured by the user
- reverse map customization
- null-safe creation of intermediate destination objects where possible
- validation when a path is not writable

Non-goals for the first pass:

- automatic unflattening
- collection item paths
- multi-source `ForPath`

### 3. Strict Validation and Diagnostics

OctoMap needs confidence-building diagnostics before it feels production-ready.

Target features:

- strict mode for unmapped destination members
- warnings for ignored or skipped members
- duplicate map detection
- duplicate member configuration detection
- configuration validation per profile/map
- projection compatibility validation
- reverse mapping validation
- better error messages for generated mapper failures

Plan inspection API:

```csharp
var plan = configuration.GetPlan<Order, OrderDto>();
```

Explain output:

```text
OrderDto.Id <- Order.Id
OrderDto.CustomerName <- Order.Customer.Name
OrderDto.TotalText <- OrderTotalTextConverter(Order.Total)
OrderDto.StatusLabel <- OrderStatusLabelResolver
```

This is important for debugging, documentation, and trust.

### 4. Conditional Mapping

Conditional mapping controls whether a member is assigned.

Target API:

```csharp
builder.CreateMap<Order, OrderDto>()
    .ForMember(x => x.Description, x => x.Condition(s => !string.IsNullOrWhiteSpace(s.Description)))
    .ForMember(x => x.Total, x => x.PreCondition(s => s.Status != "Cancelled"));
```

Semantics:

- `PreCondition(...)` runs before resolving a member value.
- `Condition(...)` runs after resolving a member value and before assignment.
- conditions should support generated expression bodies through DynaBee.

Compatibility targets:

- convention mapping
- `MapFrom(...)`
- constants
- null substitute
- converters
- resolvers
- nested mapping
- collection mapping

Projection note:

- conditions may be projectable only when expressed as LINQ-translatable expressions.

### 5. Existing Destination Mapping

OctoMap should support mapping onto an existing destination instance.

Target API:

```csharp
mapper.Map(source, existingDestination);
mapper.Map<TSource, TDestination>(source, destination);
```

Required scenarios:

- update existing DTOs
- update EF tracked entities
- partial update workflows
- preserve destination values when source values are null, when configured

Design requirements:

- generated update methods
- no destination object construction when mapping into an existing instance
- same member rules where applicable
- clear behavior for readonly members

### 6. Better Null Semantics

Current null behavior should be expanded.

Target features:

- `NullSubstitute` for nullable value types
- `AllowNullDestinationValues`
- `IgnoreNullSourceValues`
- per-profile null behavior
- per-map null behavior
- per-member null behavior
- null propagation rules for flattening and paths
- null behavior compatibility with projections

Example:

```csharp
builder.CreateMap<PatchOrderDto, Order>()
    .ForMember(x => x.Description, x => x.IgnoreNullSourceValue());
```

### 7. Type Conversion System

OctoMap needs global and reusable conversion rules.

Target features:

- global type converters
- primitive conversions
- nullable conversions
- enum-to-string
- string-to-enum
- string-to-guid
- string-to-date/time
- culture-aware conversions
- value transformers

Target API examples:

```csharp
builder.CreateConverter<string, Guid>(s => Guid.Parse(s));

builder.AddValueTransformer<string>(s => s.Trim());
```

Runtime vs projection:

- runtime converters can use DI
- projection converters must be expression-based and provider-translatable

### 8. Collection Improvements

Current collection support should expand beyond arrays, `List<T>`, and common interfaces.

Target destination shapes:

- `HashSet<T>`
- `ISet<T>`
- `Collection<T>`
- `ObservableCollection<T>`
- custom collection types with supported constructors/add methods

Target features:

- item converters
- item resolvers
- collection update instead of replacement
- preserve existing destination collection
- clear/null/merge strategies
- per-map and per-member collection policies
- projected collection mapping where LINQ providers support it

Example:

```csharp
builder.CreateMap<Order, OrderDto>()
    .ForMember(x => x.Items, x => x.CollectionMode(CollectionMapMode.Merge));
```

### 9. ReverseMap Expansion

Current `ReverseMap()` is intentionally basic.

Currently supported:

- convention direct members
- direct reversible `MapFrom(...)`
- additional reverse configuration after `.ReverseMap()`

Future reverse features:

- validation explaining what was not reversed
- `ForPath(...)` for explicit unflattening
- reverse-specific ignore rules
- reverse constructor mapping
- reverse null behavior
- reverse naming convention support

Features that should not be automatically reversed unless explicit:

- resolvers
- converters
- complex expressions
- flattening and unflattening
- multi-source maps
- lifecycle actions
- conditions with side effects or non-projectable logic

### 10. Lifecycle Actions

Lifecycle hooks are useful for object initialization and custom behaviors.

Target API:

```csharp
builder.CreateMap<Order, OrderDto>()
    .BeforeMap((source, destination, context) => ...)
    .AfterMap((source, destination, context) => ...);
```

Targets:

- per-map actions
- global actions
- DI-backed actions
- generated invocation path where possible

Projection note:

- lifecycle actions are runtime-only and should be marked non-projectable.

### 11. Inheritance and Polymorphism

Target features:

- `IncludeBase<TBaseSource, TBaseDestination>()`
- derived type maps
- interface/base destination mapping
- runtime polymorphic source dispatch
- validation for ambiguous inheritance maps

Example:

```csharp
builder.CreateMap<BaseOrder, BaseOrderDto>();

builder.CreateMap<OnlineOrder, OnlineOrderDto>()
    .IncludeBase<BaseOrder, BaseOrderDto>();
```

### 12. Open Generic Maps

Open generic maps are important for common wrappers.

Example:

```csharp
builder.CreateMap(typeof(PagedResult<>), typeof(PagedResultDto<>));
builder.CreateMap(typeof(ApiResponse<>), typeof(ApiResponseDto<>));
```

Required work:

- open generic configuration model
- closed generic map resolution
- generated mapper cache for closed generic pairs
- validation of generic constraints
- projection compatibility

### 13. Attribute Mapping

Attributes can simplify simple scenarios.

Target attributes:

- `[MapFrom]`
- `[MapTo]`
- `[IgnoreMap]`
- `[MapName]`
- `[MapConstructor]`
- `[NullSubstitute]`

Example:

```csharp
public sealed class OrderDto : IMapFrom<Order>
{
    [MapName("StatusCode")]
    public string Status { get; set; }
}
```

Rules:

- attributes should be convenience configuration
- fluent configuration must override attributes
- attribute behavior must be visible through plan inspection

### 14. Naming Conventions

Target features:

- PascalCase
- camelCase
- snake_case
- kebab-case
- prefix and suffix replacement
- source/destination naming convention pairs
- profile-level naming conventions
- map-level naming conventions

Example:

```csharp
builder.UseSourceNamingConvention(NamingConventions.SnakeCase);
builder.UseDestinationNamingConvention(NamingConventions.PascalCase);
```

### 15. Configuration Organization

Large applications need predictable configuration composition.

Target features:

- profile ordering
- profile-level options
- map override rules
- duplicate map policy
- module/package profiles
- scan filters
- explicit profile registration

### 16. Compilation Strategy

Current mapping compilation is lazy. OctoMap should also support eager compilation.

Target APIs:

```csharp
configuration.AssertValid();
mapper.CompileMappings();
mapper.CompileMap<TSource, TDestination>();
```

Targets:

- fail fast during application startup
- lazy compile by default
- warmup specific maps
- compile all configured maps
- optionally compile implicit maps discovered from known assemblies

### 17. Performance and Benchmarks

Performance must be proven, not assumed.

Benchmark targets:

- manual mapping
- AutoMapper
- Mapster
- OctoMap generated mapping
- nested mapping
- collections
- resolvers
- converters
- projections
- constructor mapping
- flattening

Metrics:

- throughput
- allocations
- cold compile cost
- warm map cost
- startup cost

### 18. Package and Release Quality

Target work:

- NuGet package metadata
- SourceLink
- XML docs package output
- symbols package
- deterministic builds
- CI test matrix
- release notes
- changelog
- public API compatibility checks
- semantic versioning policy
- README badges
- examples package or samples folder polish

### 19. Analyzer and Tooling

Potential future package:

```text
OctoMap.Analyzers
```

Analyzer ideas:

- warn when a map has unmapped required destination members
- warn when a projection uses runtime-only features
- suggest `ForMember(...)` for ambiguous maps
- detect unreachable reverse map rules
- validate attributes at compile time where possible

### 20. Advanced Projection Features

After the first projection pass:

- `ProjectTo` with external parameters
- explicit expansion
- aggregate mapping
- nested collection projection
- provider capability diagnostics
- EF Core owned entities
- queryable reverse projection where reasonable

### 21. AOT and Mobile-Friendly Backend

DynaBee is the runtime generation backend. A future mobile/AOT story may require another backend.

Potential direction:

- source-generator backend
- pre-generated mapper assembly
- interpreter-free compiled delegates where possible
- reduced reflection metadata usage
- linker-friendly configuration mode

This is why the DynaBee adapter boundary must remain clean.

## Suggested Delivery Order

The recommended order from the current state is:

1. Projection architecture and first `ProjectTo` support.
2. `ForPath(...)`.
3. Strict validation and plan inspection.
4. Conditional mapping.
5. Existing destination mapping.
6. Null semantics expansion.
7. Type conversion system.
8. Collection improvements.
9. ReverseMap expansion.
10. Lifecycle actions.
11. Inheritance and polymorphism.
12. Open generic maps.
13. Attribute mapping.
14. Naming conventions.
15. Eager compilation and warmup APIs.
16. Benchmarks.
17. Packaging and release polish.
18. Analyzer/tooling package.
19. Advanced projection features.
20. AOT/mobile-friendly backend.

## Projection Design Warning

Projection support should not be built by trying to reuse generated runtime mapper methods. Entity Framework and other LINQ providers need expression trees they can translate.

Correct boundary:

```text
Runtime mapping:
OctoMap plan -> DynaBee backend -> generated mapper type

Projection mapping:
OctoMap plan -> projection expression builder -> Expression<Func<TSource, TDestination>>
```

This will keep OctoMap fast at runtime while still supporting database-side projection.

## ReverseMap Design Warning

`ReverseMap()` should stay conservative. Automatic reverse mapping is safe only for simple, obvious cases.

When a rule is ambiguous, OctoMap should prefer explicit configuration over magic.

Good:

```csharp
.ForMember(x => x.DisplayName, x => x.MapFrom(s => s.Name))
.ReverseMap()
```

Risky:

```csharp
.ForMember(x => x.FullName, x => x.MapFrom(s => s.FirstName + " " + s.LastName))
.ReverseMap()
```

The second example should not be automatically reversed. Users should configure the reverse members explicitly.

## Minimum Alpha Bar

Before calling OctoMap broadly usable, these should exist:

- projection architecture
- `ForPath(...)`
- strict validation mode
- plan inspection
- conditional mapping
- existing destination mapping
- better type conversions
- benchmark suite
- package metadata and release docs

## Summary

OctoMap already has a strong generated-runtime mapping foundation. The next major product milestone is projection support because it defines the second execution model of the library.

The long-term shape is:

```text
One configuration model.
Two execution targets:
    - generated runtime mapping through DynaBee
    - LINQ projection expressions for EF and query providers
```

That split is the key architectural move for OctoMap to scale beyond basic object mapping.
