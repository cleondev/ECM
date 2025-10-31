# Repository Guidelines

- All .NET projects in this repository must target **.NET 9.0**. Do not downgrade project `TargetFramework` values to earlier versions.
- When adjusting code for compatibility, prefer retaining C# 13 features already in use (e.g., collection expressions, enhanced pattern matching).
- Update this file if additional cross-cutting conventions are introduced.
- Avoid referencing ASP.NET Core MVC types in shared contract libraries (e.g., `src/AppGateway/AppGateway.Contracts`). In particular, do not add `using Microsoft.AspNetCore.Mvc;` to DTOs—handle binding concerns within the API layer instead.
- When new members are added to shared interfaces (e.g., `IEcmApiClient`), update any test doubles or fakes that implement them so builds remain green.
- The Next.js frontend under `src/AppGateway/ui` is exported as a static site. Prefer approaches that remain compatible with static generation (e.g., avoid server-only features or APIs that require Node.js runtime at request time).
