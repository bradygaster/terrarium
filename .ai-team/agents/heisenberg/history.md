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

📌 Team update (2026-02-11): Diagram standards — Mermaid only, no ASCII art. All diagrams must use Mermaid — decided by Badger, bradygaster
📌 Team update (2026-02-11): VB.NET respectful framing — never refer to VB.NET negatively, just "we're C# now" — decided by bradygaster
📌 Team update (2026-02-11): CI pipeline created (.github/workflows/build.yml) targeting src/Terrarium.sln — decided by Hank
📌 Team update (2026-02-11): Solution uses classic .sln (not .slnx), CS1591 suppressed during port — decided by Heisenberg
📌 Team update (2026-02-11): Aspire packages pinned — Aspire 13.1.0, ServiceDiscovery/Resilience 10.3.0, OpenTelemetry 1.15.0 — decided by Saul
📌 Team update (2026-02-11): CSS token naming: --glass-{category}-{element}-{modifier}, BEM for components — decided by Jesse
📌 Team update (2026-02-11): Services layer (HttpClient-based, interface-first, no ServiceDefaults) — decided by Mike
📌 Team update (2026-02-11): SignalR Hub contract (8 methods, 7 callbacks, ReceiveError struct, rate limiting) — decided by Mike
📌 Team update (2026-02-11): Terrarium.Web Blazor Interactive Server (PR #118, SignalR-ready, Glass integrated) — decided by Skyler
📌 Team update (2026-02-11): Glass CSS expanded (60+ tokens, 12 new components, 76 assets cataloged) — decided by Jesse
📌 Team update (2026-02-11): Server.Tests (17 xUnit tests, 4 passing) — decided by Hank
📌 Team update (2026-02-11): SDK Samples (standalone structure, 3 creatures ported) — decided by Hank
📌 Team update (2026-02-11): Species & Reporting endpoints (assembly/filter deferred) — decided by Gus
📌 Team update (2026-02-11): Organism Isolation architecture (3-layer: static validator, AssemblyLoadContext sandbox, execution host) — decided by Heisenberg
📌 Team update (2026-02-11): Hub-and-spoke SignalR architecture finalized (rate limits, heartbeat/lease, reconnect=rejoin) — decided by Heisenberg
📌 Team update (2026-02-11): Road ahead blog post (sprint-prep-the-road-ahead.md, 48 issues, 89-minute parallelism) — decided by Beth

### 2025-07-16 — SignalR Hub-and-Spoke Architecture (Sprint 7)

- **Architecture doc created:** `docs/architecture/signalr-hub-spoke.md` — comprehensive design for SignalR-based real-time communication replacing legacy TCP P2P.
- **ITerrariumHub expanded:** Added `Heartbeat`, `RequestPeerList`, `ReportPopulation` methods. Now 8 hub methods total.
- **ITerrariumClient expanded:** Added `ReceivePeerList`, `ReceivePopulationReport`, `ReceiveError` callbacks. Now 7 client callbacks total.
- **New message types created:**
  - `Messages/PeerListResponse.cs` — active peer enumeration response
  - `Messages/PopulationReport.cs` + `SpeciesPopulation` — per-species population stats
  - `Messages/HubError.cs` — structured error delivery (replaces throwing from hub methods)
- **IEcosystemNotifier interface created:** Abstraction for grain-to-hub push notifications. Keeps `Terrarium.Orleans` decoupled from `Microsoft.AspNetCore.SignalR`. Implementation lives in `Terrarium.Server`.
- **TerrariumHub updated:** Stub implementations for all new interface methods. All TODOs reference Sprint 7 grain delegation.
- **Key architectural decisions:**
  - Hub never throws — all errors go through `ReceiveError` callback with structured `HubError` (code, transient flag, retry-after).
  - Rate limiting per connection: teleport 10/min, population 2/min, world state 5/min, peer list 2/min, heartbeat 3/min.
  - Heartbeat interval: 30 seconds client-side, 90-second lease expiry server-side (3× interval).
  - SignalR `MaximumReceiveMessageSize` set to 512 KB to accommodate assembly payloads in `CreatureTeleport`.
  - Reconnection requires re-join + re-announce (new connection ID on reconnect).
- **Key file paths:**
  - `docs/architecture/signalr-hub-spoke.md` — full architecture doc
  - `src/Terrarium.Net/ITerrariumHub.cs` — hub method contract (8 methods)
  - `src/Terrarium.Net/ITerrariumClient.cs` — client callback contract (7 callbacks)
  - `src/Terrarium.Net/IEcosystemNotifier.cs` — grain-to-hub notification abstraction
  - `src/Terrarium.Net/Messages/HubError.cs` — structured error type
  - `src/Terrarium.Net/Messages/PopulationReport.cs` — population stats message
  - `src/Terrarium.Net/Messages/PeerListResponse.cs` — peer list response message
