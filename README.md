# OctoMap

**OctoMap** is a lightweight .NET object mapping library built to generate runtime mapper types through DynaBee.

The goal is to avoid reflection-based mapping during hot execution paths. Mapping plans are expected to produce generated mapper classes that can be invoked like normal .NET types.

## Requirements

- .NET SDK 10.0+ recommended for development.
- The library multi-targets: `net8.0`, `net9.0`, and `net10.0`.

## Development

```bash
dotnet restore
dotnet build
dotnet test
```

## Benchmarks

```bash
dotnet run -c Release -f net8.0 --project benchmarks/OctoMap.Benchmarks/OctoMap.Benchmarks.csproj -- --filter *
```
