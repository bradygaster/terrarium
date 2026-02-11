# Solution Structure: Classic .sln Format, Not .slnx

**By:** Heisenberg
**Date:** 2025-07-16
**Status:** Decided

## What
The new `src/Terrarium.sln` uses the classic Visual Studio solution format (Format Version 12.00), not the new `.slnx` XML format that .NET 10 defaults to with `dotnet new sln`.

## Why
1. `.slnx` is brand new in .NET 10 and tooling support is still maturing.
2. Classic `.sln` has universal tooling support (VS, VS Code, Rider, CI systems).
3. Brady's audience includes .NET veterans who will be opening this in various IDEs.
4. `dotnet sln add` was broken by a workload manifest issue — manual `.sln` authoring was required, which is much simpler in the classic format.

## Also Decided
- **CS1591 suppressed** via `<NoWarn>CS1591</NoWarn>` in Directory.Build.props. XML doc comment warnings are suppressed during the initial port phase. Once OrganismBase and other projects have stable APIs with proper XML docs, this will be re-enabled.
- **EngineSettings fully ported** rather than stubbed — all 50+ game constants carry their original values. This is the single source of truth for game balance.
