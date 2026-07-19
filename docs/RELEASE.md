# OctoMap Release Process

This document defines the release checklist for OctoMap packages.

## Versioning

OctoMap uses semantic versioning.

- Patch versions contain compatible bug fixes.
- Minor versions contain compatible features.
- Major versions contain breaking public API or behavior changes.
- Alpha versions may change public APIs while the project is still stabilizing.

Tags use the `v` prefix:

```text
v1.0.0-alpha.1
```

## Release Checklist

Before publishing a release:

1. Update `CHANGELOG.md`.
2. Run `dotnet restore`.
3. Run `dotnet build --configuration Release`.
4. Run `dotnet test --configuration Release --no-build`.
5. Run `dotnet pack src/OctoMap/OctoMap.csproj --configuration Release --output artifacts/package`.
6. Verify that the package output includes `.nupkg` and `.snupkg`.
7. Create or publish the GitHub release from the changelog entry.

## Package Quality

The package build must include:

- NuGet metadata.
- README and icon assets.
- XML documentation.
- SourceLink.
- deterministic build settings.
- portable symbols.
- symbols package output.

## CI

Pull requests run restore, build, test, and pack validation. The build matrix validates `net8.0`, `net9.0`, and `net10.0`.

Publishing to NuGet is only allowed from a published GitHub release and requires `NUGET_API_KEY`.
