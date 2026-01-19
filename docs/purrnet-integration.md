# PurrNet Integration Guide

This document describes how Meow can integrate with PurrNet to discover and use libraries hosted on the PurrNet repository.

Base URL: `https://purrnet.example/api/v1` (replace with your real PurrNet URL)

Overview
- Search packages: `GET /packages?search={query}&page={n}&pageSize={m}`
- Get package details: `GET /packages/{name}` or `GET /packages/{name}/{version}`
- Report downloads: `POST /packages/{name}/download`

Authentication
Some endpoints may require a bearer token in the `Authorization` header:

```
Authorization: Bearer <token>
```

C# client sample

The repository includes an interface `IPurrNetService` and a basic `PurrNetService` implementation that uses `HttpClient` and async methods. Register it with DI:

```csharp
// In an ASP.NET Core or generic host startup:
services.AddHttpClient<IPurrNetService, PurrNetService>(c =>
{
    c.BaseAddress = new Uri("https://purrnet.example/api/v1/");
});
```

Example usage:

```csharp
var results = await purrNetService.SearchPackagesAsync("cli tools");
var pkg = await purrNetService.GetPackageAsync("mypackage");
await purrNetService.ReportDownloadAsync("mypackage", pkg.Version);
```

Notes
- The included client is intentionally small and opinionated: it expects JSON responses compatible with the `PurrPackage` and `PurrSearchResult` models.
- Handle API errors and authentication according to your deployment.

Dependency categories
- You can annotate dependencies in `meow.yaml` with categories so compilers can declare support for them. Example:

```yaml
dependencies:
    example-lib: "^1.0.0"
dependencyCategories:
    example-lib: "runtime"
```

Meow's `BuildService` will consult the active compiler's `SupportedDependencyCategories` list and emit warnings when a dependency's category is not declared supported by the compiler.

Next steps
- Add richer metadata mapping as needed (signatures, git URLs, manifest parsing).
- Add caching and retry policies on the `HttpClient` registration.

Automatic category population
- Running `meow install` will fetch package metadata from PurrNet and, when available, populate `dependencyCategories` in `meow.yaml`. This allows compilers to automatically know which category a dependency belongs to and prevents noisy warnings.

CLI additions
- `meow install` — resolve and save dependency metadata to `.meow/packages/` and populate categories in `meow.yaml`.
- `meow update` — refresh package metadata (currently same as `install`).
- `meow lint` — check `dependencyCategories` against the selected compiler's `SupportedDependencyCategories` and emit structured lint messages.
