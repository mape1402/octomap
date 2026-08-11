# Changelog

## [v1.1.1] - 2026-08-11

### Fixed

- Added safe constant emission for `MapFrom` and `UseValue` values including enums, primitive values, nullable primitives, `DateTime`, `DateTimeOffset`, and `Guid`.

## [v1.1.0] - 2026-08-07

### Added

- `OctoMap.Testing` package with DI registration helpers, async mapper adapter APIs, configuration validation helpers, projection helpers, and mapping assertions for profile tests.

## [v1.0.0] - 2026-07-20

### Added

- Initial OctoMap solution structure based on DynaBee.
- Library, test, benchmark, documentation, package, and CI scaffolding.
- Stable NuGet package metadata, package icon, SourceLink, deterministic builds, symbols, and release workflow support.
- Dependency injection registration through `Microsoft.Extensions.DependencyInjection`.
- Public abstraction-first API with internal runtime implementations.
- Profile-based configuration with explicit fluent maps.
- Convention-based single-source mapping by matching readable source properties to writable destination properties.
- Runtime implicit single-source map creation with compiled map caching.
- Interface-based registration through `IMapFrom<T>` and `IMapTo<T>`.
- Attribute-based registration through `[MapFrom]`, `[MapTo]`, `[MapName]`, `[NullSubstitute]`, `[IgnoreMap]`, and `[MapConstructor]`.
- Explicit multi-source maps into a single destination through `CreateMultiMap<TDestination>()` and `SourceSet`.
- Multi-source member expressions through a source context for destination members that combine values from more than one source.
- Nested object mapping with cached generated child mapper calls.
- Flattening by convention for single-source maps.
- Destination path mapping through `ForPath(...)`.
- Existing destination mapping overloads for in-place updates.
- Null source preservation and null collection handling options.
- Arrays, lists, sets, read-only collection interfaces, and collection element mapping.
- Global expression-based type converters and DI-based runtime converters.
- DI-based value resolvers, value converters, and lifecycle actions.
- Conditional mapping through `PreCondition(...)` and `Condition(...)`.
- Constructor mapping for immutable DTOs and records.
- Reverse map registration for basic reversible single-source rules.
- Base map inclusion and polymorphic lookup support.
- Open generic map registration and lazy closed-map resolution.
- LINQ projection support through `ProjectTo(..., mapper.ProjectionBuilder)`.
- Projection parameter replacement for captured `MapFrom(...)` values.
- Eager mapping compilation APIs for startup warmup and fail-fast validation.
- Configuration organization APIs for explicit profiles, map scanning, filters, duplicate policies, and diagnostics.
- Map plan diagnostics through `GetPlan(...)`, `DescribeMap(...)`, and `DescribeConfiguration()`.
- Expanded BenchmarkDotNet scenarios for runtime, compile, projection, resolver, converter, collection, constructor, nested, and flattening paths.
- `OctoMap.Analyzers` package with map attribute and runtime-only projection diagnostics.
- Competitive benchmarks against AutoMapper and Mapster for flat, cold compile, startup compile, nested, collections, constructor, flattening, and projection scenarios.
- Optimized warm single-source mapping with typed generated mapper invocation, per-registry generic map caching, context-free execution, generated child-map dependency calls, and a per-generic fast slot that avoids concurrent cache lookup on the hot path.
- Optimized collection mapping generation by caching source collection counts and generating arrays for read-only destination collection interfaces when applicable.

### Notes

- Multi-source maps are intentionally explicit and are not created implicitly at runtime.
- Runtime DI resolvers, runtime DI converters, lifecycle actions, conditional mapping, nested runtime mapping, and multi-source maps are not supported by LINQ projections.
- NativeAOT/mobile-friendly generation is not part of this release.
