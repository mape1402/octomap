# OctoMap Analyzers

`OctoMap.Analyzers` contains Roslyn diagnostics for OctoMap configuration.

The analyzer package is intentionally separate from the runtime package. Applications can reference it during development without adding runtime dependencies.

## Diagnostics

### OCMAP001: Self-Referencing Map Attribute

Emitted when `[MapFrom]` or `[MapTo]` points to the declaring type.

```csharp
[MapFrom(typeof(CustomerDto))]
public sealed class CustomerDto
{
}
```

This usually indicates a typo and creates a self map instead of a model-to-DTO map.

### OCMAP002: Runtime-Only Projection Feature

Emitted when the analyzer sees both:

- a configured map that uses APIs that cannot be translated by `ProjectTo(...)`.
- a `ProjectTo<TDestination>(...)` call for that configured destination, with the same query source type when it can be inferred.

Runtime-only APIs include:

- `ResolveUsing(...)`
- `ConvertUsing(...)`
- `BeforeMap(...)`
- `AfterMap(...)`

These APIs remain valid for object mapping. The analyzer only reports when it detects that the same map is being projected, so normal runtime-only maps do not fill the build with noise.

The diagnostic works inside a single compilation. If profiles live in one project and `ProjectTo(...)` calls live in another project, future analyzer work can add cross-project/package metadata to make this stricter.

## Future Diagnostics

Planned diagnostics include:

- required destination member coverage.
- ambiguous convention maps.
- unreachable reverse map rules.
- invalid attribute constructor arguments.
- projection provider compatibility hints.
