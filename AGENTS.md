# Blazor.Map — Project Agent Instructions

## Project Overview

**Spillgebees.Blazor.Map** is a Blazor map component library powered by [MapLibre GL JS](https://maplibre.org/).
Blazor WebAssembly is the primary target; Blazor Server works through the same protocol
(SignalR round-trip latency caps practical update rates).

## Architecture

### Solution structure

```text
Spillgebees.Blazor.Map.slnx                        # XML solution (root)
├── src/Spillgebees.Blazor.Map/                    # Razor Class Library (NuGet package)
├── src/Spillgebees.Blazor.Map.Assets/             # TypeScript/SCSS source (Vite + pnpm)
├── src/Spillgebees.Blazor.Map.Tests/              # TUnit + bUnit unit tests
├── src/Spillgebees.Blazor.Map.IntegrationTests/   # WASM host pages for Playwright (integration + perf)
└── src/Spillgebees.Blazor.Map.Docs/               # Docs/demo site (Spillgebees.Blazor.Docs.Sdk)
```

### Wire protocol (one channel)

C# components never call ad-hoc JS functions to mutate the map. All mutations are
**ops** — small JSON records (`source.add`, `layer.add`, `marker.set`,
`camera.flyTo`, …) queued on `MapEngineChannel`, flushed once per render batch to
`Engine.applyOps`, buffered until map load, and replayed after style changes. The
documented exceptions: binary motion frames and raw GeoJSON text ride a fast lane
through the same scheduler, and value-returning reads (`GetZoomAsync`,
`QueryRenderedFeaturesAsync`, …) are `Spillgebees.Engine.*` query functions.
JS→C# events flow through `MapEngineEventRouter` handler ids.

### JS/CSS build pipeline

TypeScript source lives in `src/Spillgebees.Blazor.Map.Assets/`, which has its own
`.csproj` using the `Microsoft.Build.NoTargets` SDK (single-targeted, `netstandard2.0`).
It owns the MSBuild targets (`PnpmInstall`, `PnpmBuild`, `PnpmClean`) that invoke
`pnpm install` and `vite build`, outputting to `src/Spillgebees.Blazor.Map/wwwroot/`.

The main Razor Class Library references the Assets project via `<ProjectReference>` with
`ReferenceOutputAssembly="false"` to establish a build-order dependency. This ensures pnpm
runs exactly once before any of the library's inner builds proceed.

- **Entry**: `src/index.ts` (Blazor JS initializer lifecycle hooks)
- **Bundler**: Vite (library mode, ES2022, ESM)
- **Output**: `Spillgebees.Blazor.Map.lib.module.{js,css}`
- **Linter**: Biome
- **Tests**: Vitest + jsdom

### .NET target

The library targets `net10.0` (configured in `src/General.targets`).
ASP.NET Core package versions are pinned in `src/Directory.Packages.props`.

## Quality gates (all fail the build)

- `TreatWarningsAsErrors` + `EnforceCodeStyleInBuild`: editorconfig style and naming
  rules are compile errors (private fields/properties are `_camelCase`; internal
  properties stay PascalCase).
- Public API surface is pinned by `Microsoft.CodeAnalysis.PublicApiAnalyzers`
  (`PublicAPI.Shipped.txt`/`PublicAPI.Unshipped.txt` in the library project). New or
  changed public API fails with RS0016 until the signature is added to
  `PublicAPI.Unshipped.txt`; promote Unshipped → Shipped at each release.
- The library compiles with CS1591 enabled: every public symbol needs XML docs.
- Perf budgets are enforced Playwright tests (`--project perf`), not advisory numbers.

## Testing

- **.NET**: TUnit + AwesomeAssertions + bUnit — `dotnet test --solution Spillgebees.Blazor.Map.slnx`
- **TypeScript**: Vitest + jsdom — `pnpm run test` (from `src/Spillgebees.Blazor.Map.Assets/`)
- **Browser** (from `src/Spillgebees.Blazor.Map.Assets/`, against the IntegrationTests WASM host):
  - `pnpm run test:browser:integration` — functional specs
  - `pnpm run test:browser:perf` — enforced perf budgets (`:record` to write JSON results)
  - `pnpm run test:browser:docs` — docs-site smoke tests

## Dev tooling

- **CSharpier**: formats `.cs`, `.csproj`, `.props`, `.targets`, `.slnx`, `.xml`
- **Husky.Net**: pre-commit hook runs CSharpier on staged files
- **Biome**: formats + lints TypeScript (configured in `src/Spillgebees.Blazor.Map.Assets/biome.json`)

## Code style

Soft guidelines (not build-enforced), C# side:

- **Member order**: `[Inject]` → `[CascadingParameter]` → `[Parameter]` first, then everything
  else public-before-private, with nested types and `DisposeAsync` last. Keep fields/consts at
  the top of their group. For non-component classes this is just the usual public-before-private.
