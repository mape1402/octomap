# OctoMap Architecture

OctoMap is an object mapping library inspired by AutoMapper, but designed around generated runtime code instead of reflection-heavy execution paths.

The core principle is simple:

> Configuration can inspect metadata, but mapping execution should run through generated mapper types.

Dynabee is the initial generation backend. OctoMap owns the mapping model, validation, plan building, diagnostics, conventions, and public API. Dynabee owns runtime type generation.

Dynabee must sit behind an OctoMap adapter boundary. OctoMap should never spread Dynabee-specific types across planning, runtime, configuration, or validation. This keeps the door open for another backend later, including a mobile/AOT-friendly backend.

## Goals

- Provide an AutoMapper-style developer experience.
- Generate high-performance mapper implementations.
- Keep runtime mapping free from repeated reflection.
- Support simple convention-based mapping and explicit advanced mappings.
- Support implicit mapping registration at runtime when a compatible source/destination pair is requested for the first time.
- Support mapping from multiple source objects into one destination.
- Scale to complex enterprise scenarios: profiles, modules, versioned maps, diagnostics, DI, projections, transformations, nested mappings, collections, and compile-time validation.
- Keep the architecture backend-friendly so future code generation strategies can be added without rewriting the public API.

## Non-Goals

- Do not clone AutoMapper internals.
- Do not expose Dynabee concepts directly as OctoMap's public model.
- Do not make reflection the hot path.
- Do not require users to manually instantiate generated types.
- Do not make implicit mapping a separate reflection mapper. Implicit maps must use the same planning, generation, and caching pipeline as explicit maps.

## High-Level Flow

1. User defines mapping configuration through profiles, fluent configuration, interfaces, attributes, or runtime implicit mapping.
2. OctoMap builds a semantic mapping model.
3. OctoMap validates the model and creates mapping plans.
4. OctoMap compiles mapping plans into generated mapper classes.
5. Mapper instances are cached and resolved through `IOctoMapper`.
6. Runtime calls execute generated code.

```text
User Config
    |
    v
Mapping Registry
    |
    v
Validation + Conventions
    |
    v
Mapping Plan
    |
    v
Generation Backend
    |
    v
Generated Mapper Type
    |
    v
Runtime Mapper Cache / DI
```

## Public API Layer

This is the developer-facing surface.

Primary concepts:

- `IOctoMapper`
- `IOctoMapper<TSource, TDestination>`
- `IOctoMapper<TSource1, TSource2, TDestination>`
- `IOctoProjectionBuilder`
- `OctoMapProfile`
- `IOctoMapConfiguration`
- `IOctoMapConfigurationBuilder`
- `IMapExpression<TSource, TDestination>`
- `IMultiSourceMapExpression<TDestination>`
- `IMapContext`
- `MapOptions`

Expected usage:

```csharp
services.AddOctoMap(config =>
{
    config.CreateMap<Customer, CustomerDto>()
        .ForMember(x => x.FullName, x => x.MapFrom(s => s.FirstName + " " + s.LastName))
        .ForMember(x => x.TotalOrders, x => x.MapFrom(s => s.Orders.Count));
});

var dto = mapper.Map<CustomerDto>(customer);
```

Multi-source usage:

```csharp
var dto = mapper.Map<CustomerAccountDto>(SourceSet.Of(customer, account));
```

Multi-source maps are explicit-only. OctoMap does not create multi-source maps implicitly and does not use convention matching across multiple sources because property name conflicts become ambiguous quickly.

Projection usage:

```csharp
var mapper = provider.GetRequiredService<IOctoMapper>();

var query = db.Customers.ProjectTo<Customer, CustomerDto>(mapper);
```

Profile-based usage:

```csharp
public sealed class SalesProfile : OctoMapProfile
{
    public override void Configure(IOctoMapConfigurationBuilder maps)
    {
        maps.CreateMap<Order, OrderDto>();
        maps.CreateMap<Customer, CustomerDto>();
    }
}
```

Implicit runtime usage:

```csharp
var dto = mapper.Map<CustomerDto>(customer);
```

If `Customer -> CustomerDto` is not configured and runtime implicit maps are enabled, OctoMap should build the map by convention, compile it, cache it, and execute it.

## Configuration Layer

The configuration layer captures user intent. It should not generate code directly.

Core responsibilities:

- Register type maps.
- Register multi-source type maps.
- Register member maps.
- Register nested destination path maps.
- Register conventions.
- Register global transforms.
- Register converters and resolvers.
- Register construction rules.
- Register null behavior.
- Register naming rules.
- Register profile metadata.

Important internal models:

- `TypeMap`
- `SourceSetMap`
- `MemberMap`
- `ConstructorMap`
- `ConverterMap`
- `ResolverMap`
- `TransformMap`
- `ProfileMap`
- `MapKey`

`MapKey` should include:

- source type or ordered source type set
- destination type
- optional profile name
- optional version
- optional variant/tag

This allows OctoMap to support multiple maps for the same source/destination pair without global collisions.

### Configuration Styles

OctoMap should support multiple configuration styles:

- Fluent API for full control.
- Profiles for grouped configuration.
- Interfaces for colocated map declarations.
- Attributes for simple mapping hints.
- Runtime implicit registration for convention-only maps.

Interface-based configuration:

```csharp
public sealed class CustomerDto : IMapFrom<Customer>
{
    public string Name { get; set; }
}
```

More explicit interface shape:

```csharp
public sealed class CustomerDto : IConfigureMap<Customer, CustomerDto>
{
    public void Configure(IMapExpression<Customer, CustomerDto> map)
    {
        map.ForMember(x => x.Name, x => x.MapFrom(s => s.FullName));
    }
}
```

Attribute-based configuration:

```csharp
[MapFrom(typeof(Customer))]
public sealed class CustomerDto
{
    public string Name { get; set; }
}
```

Attributes should stay lightweight. Complex mapping logic belongs in profiles, fluent configuration, resolvers, or converters.

## Convention Layer

Conventions allow OctoMap to behave like AutoMapper by default while remaining extensible.

Initial conventions:

- Exact property name match.
- Case-insensitive optional match.
- Implicit property mapping by matching readable source member names to writable destination member names.
- PascalCase flattening: `Customer.Name` -> `CustomerName`.
- Nullable-compatible assignment.
- Collection mapping when element maps exist.
- Enum-to-enum by name or value.
- Constructor parameter matching by name.

Convention pipeline:

```text
TypeMap
  -> discover source member candidates
  -> discover destination members
  -> apply naming conventions
  -> apply flattening conventions
  -> apply registered transforms
  -> produce MemberMap candidates
```

Conventions should be replaceable:

- `IMemberMatchingConvention`
- `INameTokenizer`
- `IFlatteningConvention`
- `IConstructorBindingConvention`
- `ICollectionMappingConvention`

Multi-source maps intentionally skip convention matching. Every destination member must be explicitly configured from a source contribution or from the multi-source context. This keeps ambiguous cases obvious instead of trying to guess between matching property names across several sources.

## Destination Path Mapping

`ForPath(...)` is the explicit unflattening mechanism. It records a destination property chain in the configuration model and carries that chain into the mapping plan.

```csharp
builder.CreateMap<OrderDto, Order>()
    .ForPath(x => x.Customer.Name, x => x.MapFrom(s => s.CustomerName));
```

Runtime generation creates null intermediate destination objects when their types are concrete, writable, readable, and expose public parameterless constructors. Projection support for destination paths is a separate feature because query providers need nested `MemberInit` expressions instead of imperative null checks and assignments.

## Implicit Runtime Mapping

OctoMap should support runtime map registration when no explicit map exists.

Example:

```csharp
var dto = mapper.Map<CustomerDto>(customer);
```

If `Customer -> CustomerDto` is not configured, OctoMap should:

1. Build an implicit `TypeMap`.
2. Apply default conventions.
3. Validate the generated plan according to runtime implicit-map rules.
4. Compile the mapper through the configured generation backend.
5. Store the compiled mapper in cache.
6. Execute the mapping.

Runtime implicit maps should be configurable:

```csharp
services.AddOctoMap(options =>
{
    options.EnableRuntimeImplicitMaps = true;
    options.ImplicitMapValidation = ImplicitMapValidationMode.Strict;
});
```

Suggested validation modes:

- `Strict`: fail on unmapped writable destination members.
- `Lenient`: map what can be mapped and ignore the rest.
- `ThrowOnAmbiguity`: allow missing members but fail ambiguous matches.

Implicit runtime registration must share the same planning and generation pipeline as explicit maps. It should not be a separate reflection mapper.

## Planning Layer

The planning layer transforms configuration into an executable plan. This is OctoMap's most important internal boundary.

The plan is independent from Dynabee.

Core responsibilities:

- Resolve every destination member.
- Resolve source ownership for single-source and multi-source maps.
- Decide how each value is produced.
- Decide construction strategy.
- Decide null handling.
- Decide nested map calls.
- Decide converter/resolver calls.
- Decide collection loops.
- Decide conditional assignment and precondition rules.
- Decide before/after map actions.
- Produce diagnostics.

Core models:

- `MappingPlan`
- `SourceBindingPlan`
- `ObjectConstructionPlan`
- `MemberAssignmentPlan`
- `ValueResolutionPlan`
- `NestedMappingPlan`
- `CollectionMappingPlan`
- `ConditionPlan`
- `NullSubstitutionPlan`
- `AfterMapPlan`
- `BeforeMapPlan`

`ValueResolutionPlan` variants:

- direct source member
- source member chain
- direct source member from a selected source object
- expression
- constant
- custom resolver
- custom converter
- nested mapper
- collection mapper
- context item

This planning layer is what lets OctoMap scale beyond one generation backend.

For multi-source maps, every member assignment must record which source parameter owns the value:

```text
Destination.FullName <- source[0].FullName
Destination.Balance  <- source[1].Balance
```

## Generation Layer

The generation layer receives a `MappingPlan` and emits an implementation.

Primary abstraction:

```csharp
public interface IMappingPlanCompiler
{
    CompiledMap Compile(MappingPlan plan);
}
```

This abstraction is the adapter boundary. Dynabee is only one implementation of this contract.

Backend-facing abstractions:

```csharp
public interface IMappingGenerationBackend
{
    string Name { get; }
    bool Supports(MappingPlan plan);
    CompiledMap Compile(MappingPlan plan);
}
```

Possible implementations:

- `DynabeeMappingGenerationBackend`
- `ExpressionMappingGenerationBackend`
- `SourceGeneratorMappingGenerationBackend`
- `AotMappingGenerationBackend`

OctoMap internals should depend on `IMappingGenerationBackend`, not on Dynabee directly.

Dynabee implementation:

```text
DynabeeMappingPlanCompiler
  -> creates dynamic class
  -> emits Map(TSource source, IMapContext context)
  -> emits MapToExisting(TSource source, TDestination destination, IMapContext context) for single-source update maps
  -> emits Map(source1, source2, ..., IMapContext context) for multi-source maps
  -> creates backend-neutral compiled method invokers
  -> emits direct IL for property reads/writes
  -> emits calls to generated nested mappers when needed
```

Generated mapper shape:

```csharp
internal sealed class GeneratedCustomerMapper
{
    TDestination Map(TSource source, IMapContext context);
    TDestination MapToExisting(TSource source, TDestination destination, IMapContext context);
}
```

Multi-source generated mapper shape:

```csharp
internal sealed class GeneratedOrderSummaryMapper
{
    TDestination Map(TSource1 source1, TSource2 source2, IMapContext context);
}
```

Generated classes are intentionally not part of the public abstraction surface. `CompiledMap` exposes backend-neutral invokers for creation and existing-destination mapping, while public `IOctoMapper` and `IOctoMapper<TSource, TDestination>` remain ordinary injectable OctoMap services.

For source-set scenarios, OctoMap routes through an internal compiled map entry, but generated method signatures should still be typed whenever possible.

The generated mapper should be a normal class:

- constructor dependencies are injected when needed
- nested mappers can be dependencies
- custom resolvers can be dependencies
- converters can be dependencies
- no user code needs to know the generated type name

## Runtime Layer

The runtime layer is the execution facade.

Primary types:

- `IOctoMapper`
- `OctoMapper`
- `ICompiledMapRegistry`
- `IMapperCache`
- `IMapContextFactory`

Responsibilities:

- Resolve the compiled mapper by source/destination pair.
- Resolve the compiled mapper by source-set/destination pair.
- Create map contexts.
- Handle object/object overloads.
- Route generic calls through backend-neutral compiled map invokers.
- Cache compiled map delegates or mapper instances.
- Surface friendly runtime errors.
- Create implicit maps on demand when enabled.

Runtime API:

```csharp
TDestination Map<TDestination>(object source);
TDestination Map<TSource, TDestination>(TSource source);
TDestination Map<TSource, TDestination>(TSource source, TDestination destination);
TDestination Map<TDestination>(SourceSet sources);
object Map(object source, Type sourceType, Type destinationType);
object Map(IReadOnlyList<object> sources, Type destinationType);
```

## Validation Layer

Validation must happen before generation whenever possible.

Validation categories:

- unmapped destination members
- ambiguous member matches
- ambiguous multi-source member matches
- unsupported conversions
- missing nested maps
- missing constructors
- invalid custom expressions
- cyclic maps
- unsupported collection shapes
- nullability risk
- inaccessible members

Validation result:

- errors
- warnings
- generated plan summary
- member-level diagnostics

API:

```csharp
configuration.AssertValid();
configuration.Validate(out OctoMapValidationReport report);
```

## Diagnostics Layer

OctoMap should be explainable. This is where it can go beyond AutoMapper for debugging and performance visibility.

Diagnostics should support:

- print mapping plan
- print generated type names
- list generated maps
- explain why a member mapped a certain way
- show unmapped members
- show performance-sensitive fallbacks
- show generated backend used

API ideas:

```csharp
var report = configuration.DescribeMap<Customer, CustomerDto>();
var plan = configuration.GetPlan<Customer, CustomerDto>();
```

The first diagnostics pass exposes validated mapping plans and a replaceable `IMappingPlanDescriber`. Diagnostics consume `MappingPlan`; they must not depend on DynaBee or generated runtime type details.

## Extensibility Layer

OctoMap should support extension points without exposing internal mutability everywhere.

Extension interfaces:

- `IValueResolver<TSource, TDestination, TValue>`
- `ITypeConverter<TSource, TDestination>`
- `IMemberValueConverter<TSourceMember, TDestinationMember>`
- `IMappingAction<TSource, TDestination>`
- `IMappingConvention`
- `IMappingPlanRule`
- `IMappingPlanCompiler`
- `IMappingGenerationBackend`
- `IMapCacheKeyStrategy`

This is the layer that allows:

- EF-style projection extensions
- JSON mapping extensions
- validation extensions
- source generator backend later
- mobile/AOT backend later
- specialized collection mappers

## Dependency Injection Layer

DI should be first-class, similar to Dynabee.

Registration styles:

```csharp
services.AddOctoMap(typeof(SalesProfile).Assembly);
services.AddOctoMap(config => { ... });
services.AddOctoMapProfiles(ServiceLifetime.Singleton, assemblies);
```

Generated mappers should be registered as:

- `IOctoMapper<TSource, TDestination>`
- `IOctoMapper<TSource1, TSource2, TDestination>` for typed multi-source maps
- internal compiled map entries
- optionally concrete generated types for diagnostics only

Default lifetime:

- configuration: singleton
- compiled registry: singleton
- generated mappers: singleton unless dependencies require scoped
- runtime `IMapContext`: transient/per-call

## Caching Strategy

OctoMap needs several caches:

- configuration hash
- map key -> mapping plan
- plan hash -> compiled mapper type
- map key -> mapper instance/delegate
- source-set map key -> mapper instance/delegate
- source/destination member metadata cache

Important rule:

Reflection is allowed during configuration and planning. It is not allowed repeatedly during mapping execution.

Cache key should include:

- source type or ordered source type set
- destination type
- profile/version/variant
- implicit or explicit registration mode
- relevant options
- configuration fingerprint
- backend version

## Advanced Mapping Capabilities

OctoMap should eventually support:

- nested object mapping
- collections and arrays
- dictionaries
- flattening and unflattening
- constructor mapping
- immutable destination types
- records
- init-only properties
- private setters by opt-in
- enum strategies
- conditional members
- preconditions
- null substitution
- value converters
- type converters
- before/after map actions
- context items
- max depth
- preserve references
- polymorphic maps
- open generic maps
- profile-specific maps
- versioned maps
- multi-source maps
- runtime implicit maps
- interface-based map declarations
- attribute-based map hints

## Projection Support

AutoMapper has `ProjectTo`. OctoMap supports a first projection pass through a separate pipeline.

Projection should not reuse runtime IL mapping blindly because query providers need expression trees.

Current model:

```text
Runtime Map Plan -> Generated IL Mapper
Projection Plan  -> Expression<Func<TSource, TDestination>>
```

Public API:

```csharp
Expression<Func<TSource, TDestination>> projection =
    projectionBuilder.Build<TSource, TDestination>();

IQueryable<TDestination> query =
    source.ProjectTo<TSource, TDestination>(mapper);
```

`IOctoMapper` is the ergonomic projection facade for application code. The projection builder depends on OctoMap configuration, validation, and planning. It does not depend on DynaBee or `IMappingGenerationBackend`.

First-pass projection supports:

- direct member assignment
- configured `MapFrom(...)`
- constants
- null substitutes
- convention flattening
- constructor projection

Runtime-only features such as DI resolvers, DI converters, nested runtime mapping, collection runtime mapping, and multi-source maps are rejected with clear errors.

## Backend Strategy

Initial backend:

- Dynabee runtime generation.

Future possible backends:

- expression tree compilation
- Roslyn source generation
- cached C# code generation
- AOT-safe backend

The backend boundary should prevent OctoMap from becoming tied to one generation mechanism.

Backend selection should be policy-driven:

```text
MappingPlan -> backend capability check -> selected backend -> compiled map
```

This is especially important for mobile or AOT scenarios where `Reflection.Emit` may not be available.

## Suggested Internal Project Layout

Initial single-project layout:

```text
src/OctoMap
  Abstractions/
  Configuration/
  Conventions/
  DependencyInjection/
  Diagnostics/
  Generation/
    Contracts/
    Dynabee/
  Planning/
  Runtime/
  Validation/
```

Split into multiple packages later only when needed:

```text
OctoMap
OctoMap.Abstractions
OctoMap.DependencyInjection
OctoMap.Dynabee
OctoMap.Projections
```

Start as one package to keep the API cohesive.

## MVP Sequence

### Phase 1: Core Runtime Mapping

- `CreateMap<TSource, TDestination>()`
- convention-based property mapping
- implicit runtime `Map<TDestination>(source)` map creation
- generated `IOctoMapper<TSource, TDestination>`
- default constructor destination creation
- simple assignable property copies
- mapper registry
- `Map<TSource, TDestination>()`
- validation for unmapped destination properties
- generation backend contract with Dynabee adapter

### Phase 2: Fluent Member Rules

- `ForMember`
- `Ignore`
- `MapFrom`
- constants
- null substitution
- conditions
- interface-based mapping declarations
- attribute-based mapping declarations

### Phase 3: Nested and Collections

- nested maps
- list/array mapping
- collection element mapper reuse
- cyclic map detection
- multi-source maps
- generated typed multi-source mappers

### Phase 4: DI and Extensibility

- resolvers
- converters
- before/after actions
- profile discovery
- mapper lifetimes
- configurable implicit-map validation modes

### Phase 5: Diagnostics and Benchmarks

- plan reports
- generated type reports
- benchmark suite vs manual mapping, reflection mapping, and AutoMapper

### Phase 6: Projections

- expression projection builder
- IQueryable integration
- projection compatibility diagnostics
- nested and collection projections where provider-friendly

## Design Principles

- Public API should feel familiar to AutoMapper users.
- Internal architecture should not mirror AutoMapper.
- Plans are the source of truth.
- Generated code is an implementation detail.
- The generation backend is replaceable.
- Reflection belongs in configuration/planning, not execution.
- Diagnostics must explain every generated decision.
- Every feature should be benchmarkable.
- Fail early during validation whenever possible.
