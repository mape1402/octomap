# OctoMap Release Process

This document defines the release checklist for OctoMap packages.

## Versioning

OctoMap uses semantic versioning.

- Patch versions contain compatible bug fixes.
- Minor versions contain compatible features.
- Major versions contain breaking public API or behavior changes.
- Alpha versions may change public APIs while the project is still stabilizing.

Production deployments run from a release branch:

```text
releases/v1.0.0
```

The workflow creates the matching tag after validating the branch and changelog.

## Release Checklist

Before publishing a release:

1. Create a branch named `releases/vX.Y.Z` from `main`.
2. Update `CHANGELOG.md` with a `## [vX.Y.Z]` section.
3. Run `dotnet restore`.
4. Run `dotnet build --configuration Release`.
5. Run `dotnet test --configuration Release --no-build`.
6. Run `dotnet pack src/OctoMap/OctoMap.csproj --configuration Release --output artifacts/package`.
7. Run `dotnet pack src/OctoMap.Analyzers/OctoMap.Analyzers.csproj --configuration Release --output artifacts/package`.
8. Run the `Release to NuGet` workflow manually from the release branch.

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

Pull requests and pushes to `main` run restore, build, and test. This is the trunk-based validation path.

Publishing to NuGet is only allowed from a manually dispatched workflow on a branch named `releases/vX.Y.Z` and requires `NUGET_API_KEY`.
