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

Emitted when configuration uses APIs that cannot be translated by `ProjectTo(...)`.

Runtime-only APIs include:

- `ResolveUsing(...)`
- `ConvertUsing(...)`
- `BeforeMap(...)`
- `AfterMap(...)`

These APIs remain valid for object mapping. The analyzer only points out that LINQ providers cannot translate them during projection.

## Future Diagnostics

Planned diagnostics include:

- required destination member coverage.
- ambiguous convention maps.
- unreachable reverse map rules.
- invalid attribute constructor arguments.
- projection provider compatibility hints.
