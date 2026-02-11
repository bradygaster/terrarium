# .NET Terrarium Is Back: Bringing a 25-Year-Old .NET Classic to .NET 10

> **Status:** Living draft — updated each sprint. This will become the final announcement post.

---

*This post is being written as we build. Each sprint adds to the story. When Terrarium is running on .NET 10 in a browser, this post ships.*

---

## The Game That Taught a Generation to Code in .NET

If you were writing .NET code in the early 2000s, you remember Terrarium.

<!-- TODO: Expand with PDC history, screenshots, community stories -->

## The Challenge: .NET Framework 1.0 → .NET 10

Twenty-five years. That's how long Terrarium has been waiting for this upgrade.

The original codebase is a time capsule of early .NET — and everything in it is either deprecated or removed in modern .NET:

| What the original used | Status in .NET 10 | What we replaced it with |
|------------------------|-------------------|--------------------------|
| `BinaryFormatter` | **Removed** (security vulnerability) | `System.Text.Json` |
| `[Serializable]` / `[NonSerialized]` | Obsolete | Explicit JSON serialization contracts |
| `Hashtable` / `ArrayList` | Superseded | `Dictionary<TKey, TValue>` / `List<T>` |
| Code Access Security (CAS) | **Removed** | `AssemblyLoadContext` + process isolation |
| AppDomains | **Removed** in .NET Core+ | Process isolation with anonymous pipes |
| DirectDraw 7 COM interop (`DxVBLib`) | Dead | HTML5 Canvas via Blazor JS interop |
| ASMX SOAP web services | Not in ASP.NET Core | ASP.NET Core Minimal APIs |
| `DataSet` over SOAP | Dead weight | Dapper + stored procedures + JSON |
| Windows Forms UI | Desktop-only | Blazor Interactive Server |

There had been two previous modernization attempts. Someone in 2010 created a WPF client project, scaffolded 13 class libraries, set them all to .NET Framework 4.0, and... never wrote the code. Eleven of those 13 projects contain exactly one file: `Properties/AssemblyInfo.cs`. We're attempt number three. We don't intend to stop at the scaffolding.

### Sprint 0: The Foundation

The modernization started with five AI agents working in parallel, each on their own branch, each producing a PR:

**The solution structure.** A new `Terrarium.sln` (finally spelled correctly — the original was `Terrraium2010.sln`, three R's, since Visual Studio 2010) with SDK-style projects, `Directory.Build.props` setting `net10.0`, nullable reference types, and warnings-as-errors.

**The creature SDK.** `Terrarium.OrganismBase` — 91 source files ported from .NET Framework 3.5 to .NET 10. Every creature attribute, every action class, every state object. `[Serializable]` stripped from 28 classes. File-scoped namespaces. Nullable annotations. The creature API surface — `class MyCarnivore : Animal` with point-based attribute allocation — preserved exactly as developers remember it.

**The visual identity.** Every `Color.FromArgb()` call from the original Glass UI framework extracted into CSS custom properties. `Color.FromArgb(128, 128, 255)` → `--glass-gradient-panel-top: #8080ff`. 153 lines of design tokens that preserve the exact visual identity of Terrarium — the blue-to-dark-blue panel gradients, the green button hover glow, the glass overlay sheen — now as standard CSS that works in any browser.

**.NET Aspire orchestration.** An AppHost ready to wire up the server and frontend. `dotnet run --project src/Terrarium.AppHost` will start the Aspire dashboard with service discovery, health checks, and OpenTelemetry.

**CI from day one.** GitHub Actions building and testing on every push and PR. Ubuntu runner — this isn't a Windows app anymore.

### Sprint 1: Server Bootstrap

The server came online. Seven legacy ASMX web services — SOAP/XML endpoints accepting `DataSet` payloads over the wire — began their migration to ASP.NET Core Minimal APIs returning JSON. The messaging endpoints (`/welcome`, `/motd`, `/version`) were the first to port: `[WebMethod]` attributes replaced by lambda expressions, SOAP envelopes replaced by `Results.Ok()`.

The original rate limiter — a creative hack built on ASP.NET's `HttpContext.Current.Cache` with eviction callbacks to decrement request counters — was ported to `IMemoryCache` with `ConcurrentDictionary` and proper middleware that returns HTTP 429 with `Retry-After` headers.

SQL Server joined the Aspire AppHost as a container resource with persistent lifetime. `dotnet run` now starts the server *and* the database. Dapper and `Microsoft.Data.SqlClient` are wired for the ~17 stored procedures that have been battle-tested for two decades.

The biggest story: Heisenberg evaluated the networking architecture and discovered that the original team had unknowingly built an actor system — static `Hashtable` state management, lease timeouts, manual serialization for crash recovery. His recommendation: Orleans + SignalR hybrid with four grain types (EcosystemGrain, PeerGrain, SpeciesRegistryGrain, PopulationGrain). Orleans formalizes what the legacy code was doing by hand. This decision reshapes Sprints 7-12.

New team member: **Badger** (Diagram Designer) — converted all ASCII diagrams to Mermaid and established diagram standards repo-wide.

## The Architecture: Modern .NET, Cross-Platform, In Your Browser

- **Blazor** for the UI
- **.NET Aspire** for orchestration
- **SignalR** for real-time creature interaction
- **HTML5 Canvas** for rendering
- **Azure Container Apps** for deployment

<!-- TODO: Deep dive into each choice and WHY -->

## The AI Story: A Squad of Agents Did This

This modernization is being executed by a squad of AI agents — each with a defined role, clear ownership, and their own branch.

In Sprint 0, five agents launched simultaneously:
- **Heisenberg** (Architect) built the solution structure and `Directory.Build.props`
- **Mike** (Engine/Networking) ported 91 OrganismBase source files from .NET 3.5 to .NET 10
- **Jesse** (Sprite/Assets) extracted every color value from the Glass UI into CSS custom properties
- **Saul** (DevOps/Aspire) wired up .NET Aspire AppHost and ServiceDefaults
- **Hank** (QA) created the GitHub Actions CI pipeline

Each agent read the legacy code, created new files, worked on their own branch, and created their own PR. They didn't coordinate in real time — they each read the sprint plan and the architecture doc, then went and did their job. Five PRs, all building, all merging cleanly.

This is the pattern: parallel workstreams with clear boundaries, shared conventions, and a decision log that keeps everyone aligned. The agents move faster than humans. The pattern is the same one that works with human teams.

<!-- TODO: Expand with later sprint stories as they complete -->

## The Result

<!-- TODO: Screenshots, running app, creature SDK examples -->

## Try It Yourself

<!-- TODO: azd up instructions, creature SDK quickstart, join the ecosystem -->

---

*Written by the Terrarium modernization team. Built with .NET 10, Blazor, .NET Aspire, and a whole lot of love for this community.*
