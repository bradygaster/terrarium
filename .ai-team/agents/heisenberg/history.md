# Project Context

- **Owner:** bradygaster (bradyg@microsoft.com)
- **Project:** .NET Terrarium 2.0 — peer-to-peer networked creature ecosystem game
- **Stack:** C#, .NET, WPF, ASP.NET MVC, DirectX, P2P networking
- **Created:** 2026-02-10

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2025-07-15 — Solution Architecture Scan

- **Three generations coexist:** .NET 2.0 (Samples/SDK/Tools), .NET 3.5 (Client\ — the real code), .NET 4.0 (ClientWPF\/ServerMVC\ — abandoned rewrite shells).
- **The VS 2010 rewrite (`ClientWPF\`) was never completed.** 11 of 13 client projects contain only AssemblyInfo.cs. The actual game logic lives in `Client\`.
- **Active solution file:** `Terrraium2010.sln` (note the typo — three R's). Contains 15 projects in Server/Client solution folders.
- **Legacy solution files:** `Client\client.sln` (VS 2008 WinForms client), `Server\server.sln` (VS 2008 WebForms server).
- **No NuGet, no DI, no global build props.** All deps are framework assemblies or direct DLL references.
- **Strong naming** via `Keys\development.snk` on all legacy client assemblies.
- **Shared version info** via linked `Client\VersionInfo.cs`.
- **DirectX dependency** is the hardest modernization blocker — `Client\Renderer\` references `DXVBLib.dll` (DX7 COM interop).
- **Server communication** uses ASMX Web References in `Client\Services\` (BugReporting, Charts, Discovery, Messaging, Reporting, Species, Usage, Watson).
- **Legacy client dependency order (leaves first):** OrganismBase, Glass, Services → HttpListener, Configuration → Controls → Game → Renderer → Terrarium.
- **Key file paths:**
  - `ARCHITECTURE.md` — full solution architecture overview (created by me)
  - `Client\Terrarium\terrarium.csproj` — the main WinForms client executable (most project references)
  - `Client\Game\Game.csproj` — game engine (engine, hosting, P2P networking)
  - `Client\OrganismBase\OrganismBase.csproj` — organism SDK (actions, state, attributes, interfaces)
  - `Client\Renderer\Renderer.csproj` — DirectX rendering layer
  - `Client\Services\Services.csproj` — server communication via Web References
  - `Server\Website\` — original ASP.NET WebForms/ASMX server
  - `ServerMVC\TerrariumServer\` — MVC 2 scaffold (HomeController, AccountController only)

📌 Team update (2026-02-10): MVC Server is a scaffold — all game logic lives in legacy ASMX — decided by Gus
📌 Team update (2026-02-10): Build must be green before new tests — decided by Hank
📌 Team update (2026-02-10): .NET 10 modernization sprint plan created — 14 sprints, WPF on .NET 10, Silk.NET OpenGL, gRPC P2P, Dapper+stored procs, process isolation, xUnit, System.Text.Json — decided by Heisenberg
📌 Team update (2026-02-11): Beth's voice is fearless, developer-first, community-driven — decided by bradygaster

### 2025-07-15 — .NET 10 Modernization Sprint Plan

- **14-sprint plan created** (~7 months at 2-week sprints) for .NET 3.5 → .NET 10 migration.
- **Key architectural decisions made:**
  - New clean solution with SDK-style `.csproj` (not in-place migration of VS 2008/2010 projects)
  - WPF on .NET 10 for UI (not MAUI or Avalonia — game is Windows-only, WPF port already partially exists)
  - Silk.NET (OpenGL) replaces DirectX 7 DirectDraw — with SkiaSharp as fallback
  - ASP.NET Core Minimal APIs for server (replacing ASMX SOAP services)
  - Dapper + existing stored procedures (not EF Core — sprocs are stable, no reason to rewrite)
  - gRPC replaces custom TCP for P2P networking (structured protocol, binary serialization, TLS)
  - `System.Text.Json` replaces `BinaryFormatter` everywhere (BinaryFormatter removed in .NET 10)
  - Process isolation replaces CAS/AppDomain sandboxing for creature code execution
  - C# port of native C++ AsmCheck using `System.Reflection.Metadata` for IL validation
  - xUnit for test framework
- **Sprint plan rationale:** Leaf-to-root ordering (OrganismBase first, main client last). Server work fully parallelized with client work. Security sandboxing (Sprint 6) and Renderer (Sprint 8) identified as highest-risk sprints.
- **Flagged 6 decisions for Brady's input:** SQL hosting, deployment target, VB.NET support, legacy code disposition, sprite assets, cross-platform aspirations.
- **Written to:** `MODERNIZATION.md` in repo root

### 2025-07-16 — .NET 10 Solution Structure Created (Sprint 0)

- **New solution:** `src/Terrarium.sln` (classic `.sln` format, not `.slnx`) with 6 SDK-style projects.
- **global.json** at repo root pins .NET SDK 10.0.103 with `rollForward: latestFeature`.
- **Directory.Build.props** in `src/` sets `net10.0`, nullable, implicit usings, TreatWarningsAsErrors, NoWarn CS1591 (XML doc comments suppressed during initial port).
- **Project structure:**
  - `Terrarium.OrganismBase` — class library (namespace `OrganismBase`). Mike's partial port includes enums, interfaces, actions, event args, exceptions, attributes, and state stubs.
  - `Terrarium.Game` — class library, references OrganismBase.
  - `Terrarium.Server` — ASP.NET Core web project (Microsoft.NET.Sdk.Web), minimal Program.cs.
  - `Terrarium.Web` — Blazor Interactive Server project (Microsoft.NET.Sdk.Web), minimal Program.cs.
  - `Terrarium.AppHost` — .NET Aspire AppHost using `Aspire.AppHost.Sdk/13.1.0` (no workload needed on .NET 10).
  - `Terrarium.ServiceDefaults` — Aspire shared project with OpenTelemetry, health checks, service discovery, resilience.
- **EngineSettings fully ported** from `Client/OrganismBase/Classes/Engine/EngineSettings.cs` — all 50+ game constants preserved.
- **Aspire SDK migration:** `IsAspireHost` property and workload-based approach replaced with `Aspire.AppHost.Sdk/13.1.0` as project SDK — workloads deprecated in .NET 10.
- **Key convention:** All new code goes under `src/`. Legacy code stays untouched.
📌 Team update (2025-07-15): CSS tokens use `--glass-{category}-{element}-{modifier}` naming; BEM classes; `glass-theme.css` is single source of truth — decided by Jesse
📌 Team update (2026-02-10): Keep ArrayList on Scan() until Game project ported — decided by Mike
### 2025-07-16 — Orleans + SignalR Architecture Evaluation

- **Recommendation: YES to Orleans + SignalR (hybrid).** Orleans owns stateful domain logic; SignalR remains as browser push channel only.
- **Grain model:** `EcosystemGrain` (tick loop, world state, teleportation mediation), `PeerGrain` (lease management, heartbeat, bad-peer tracking), `SpeciesRegistryGrain` (species CRUD, throttling, assembly storage), `PopulationGrain` (write-behind population reporting).
- **Key insight:** The legacy codebase already implements actor patterns manually — `ReportingService._lastGuid` Hashtable, `PeerManager` with lease timeouts, `GameEngine` with state serialization. Orleans names and formalizes these patterns.
- **OrganismGrain rejected:** Per-organism grains are wrong for Terrarium. The 10-phase `ProcessTurn` loop requires atomic tick processing across all ~300 organisms with spatial collision detection (`GridIndex`). `EcosystemGrain` holding `WorldState` is the right granularity.
- **Sprint impact:** Sprint 7 gets heavier (Orleans setup + grain implementation), Sprint 11 gets lighter (Orleans handles scaling, no Redis/gRPC needed). `SignalR.Orleans` provides backplane without Redis.
- **Aspire integration:** `AddOrleans()` in AppHost is first-class. Azure Table/Blob Storage for clustering and grain state in production.
- **Written to:** `.ai-team/decisions/inbox/heisenberg-orleans-evaluation.md`

📌 Team update (2025-07-16): Orleans + SignalR hybrid recommended — Orleans for stateful domain (ecosystems, peers, species), SignalR as thin browser push channel — decided by Heisenberg
