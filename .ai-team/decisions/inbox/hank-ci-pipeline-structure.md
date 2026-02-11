# Decision: CI Pipeline Structure

**By:** Hank (Tester/QA)
**Date:** 2025-07-15
**Issue:** #5

## What

Created a single `.github/workflows/build.yml` that handles both push CI and PR status checks. No separate `pr-check.yml` was created.

## Key Choices

1. **Single workflow, not two.** The build workflow triggers on pushes to `main`/`squadified` AND on all PRs. A separate PR-check workflow would be ceremony — there are no label conventions, title formats, or branch policies to enforce yet. When those exist, a second workflow can be added.

2. **`dotnet-quality: preview`** — .NET 10 is in preview. This flag ensures the SDK installs correctly from the preview channel. Remove this once .NET 10 goes GA.

3. **`global.json` detection** — The workflow checks for `global.json` at repo root. If present, it pins the SDK version from that file. If absent, it falls back to `10.0.x`. This means Heisenberg can drop a `global.json` anytime and CI will respect it automatically.

4. **Solution path: `src/Terrarium.sln`** — This targets Heisenberg's new .NET 10 solution structure, NOT the legacy `Terrraium2010.sln`. CI will fail until the solution exists, which is expected and correct.

5. **NuGet cache key** — Keyed on `**/*.csproj`, `**/Directory.Packages.props`, and `**/Directory.Build.props` to support central package management if/when adopted.

## Why This Matters

- Every PR now gets a build+test status check — no more flying blind
- Cache keeps builds fast as the project grows
- The workflow is intentionally minimal and correct — easy to extend later
