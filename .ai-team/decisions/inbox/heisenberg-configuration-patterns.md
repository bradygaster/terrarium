# Decision: Terrarium.Configuration Patterns

**By:** Heisenberg (Lead / Architect)
**Date:** 2025-07-16
**Status:** Implemented (PR for #15)

## What

1. **`IOptions<GameSettings>` replaces static `GameConfig`.** All game configuration goes through `IOptions<GameSettings>` / `IOptionsSnapshot<GameSettings>` / `IOptionsMonitor<GameSettings>`. No more static mutable singletons. The `"Game"` section of `appsettings.json` is the binding target.

2. **DataAnnotations validation on start.** `ValidateOnStart()` catches misconfiguration before the app serves traffic. Currently validates `CpuThrottle` range (50–200).

3. **`ErrorLog` is an injected service, not a static class.** Takes `ILogger<ErrorLog>` via constructor. No `Mutex`, no `DataSet`, no `System.Windows.Forms`.

4. **`TimeMonitor` uses `Stopwatch`, not P/Invoke.** `QueryPerformanceCounter`/`QueryPerformanceFrequency` replaced with `System.Diagnostics.Stopwatch`. Same API shape (`Start()`, `EndGetMicroseconds()`, `GetCounterSeconds()`).

5. **WinForms UI classes not ported.** `TerrariumTraceListener`, `ReportBug`, and `CachedBooleanConfig` are legacy artifacts. The trace listener depended on `System.Windows.Forms.Timer`; the bug reporter was a WinForms dialog calling ASMX. These belong to a future UI layer, not a configuration library.

## Why

The original `GameConfig` was a static class reading/writing XML via `XmlDocument` to `~/Documents/Terrarium/userconfig.xml`. Every property had its own cached field with lazy initialization — classic .NET 1.x pattern. Moving to `IOptions<T>` makes the configuration injectable, testable, and compatible with the standard ASP.NET Core configuration pipeline (`appsettings.json`, environment variables, Azure App Configuration, etc.).

## Impact

- Any project needing game configuration should reference `Terrarium.Configuration` and inject `IOptions<GameSettings>`.
- Call `services.AddTerrariumConfiguration()` in your DI setup.
- The `Profiler`/`ProfilerNode`/`TimeMonitor` classes are available for any project that needs performance measurement.
