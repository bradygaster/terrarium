# Project Context

- **Owner:** bradygaster (bradyg@microsoft.com)
- **Project:** .NET Terrarium 2.0 — peer-to-peer networked creature ecosystem game
- **Stack:** C#, .NET, WPF, ASP.NET MVC, DirectX, P2P networking
- **Created:** 2026-02-10

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-02-10 — Initial Server Domain Exploration

**MVC Server (`ServerMVC/TerrariumServer/`)**
- Scaffold only. ASP.NET MVC 2 on .NET 4.0. Has Account + Home controllers, zero game logic.
- `Controllers/AccountController.cs` — LogOn, LogOff, Register, ChangePassword using DI-friendly `IFormsAuthenticationService` / `IMembershipService`
- `Models/AccountModels.cs` — Auth models + service wrappers + validation attributes
- `Global.asax.cs` — Default `{controller}/{action}/{id}` routing
- Tests: 15 MSTest methods in `TerrariumServer.Tests/Controllers/` covering Account + Home controllers with mock services

**Legacy Server (`Server/Website/`) — The Real Server**
- ASP.NET 2.0 Web Site model (not Web Application). All code in `App_Code/`.
- 7+ ASMX web services — these are the API contracts clients depend on:
  - `Discovery/discoverydb.asmx` → `PeerDiscoveryService` — peer registration, counting, version kill-switch
  - `Species/addspecies.asmx` → `SpeciesService` — creature upload (byte[] assembly), retrieval, reintroduction, blacklist
  - `Reporting/reportpopulation.asmx` → `ReportingService` — population data per tick per species
  - `Messaging/messaging.asmx` → `Messaging` — welcome message, MOTD, latest version
  - `Watson/watson.asmx` → `WatsonService` — crash/error reporting
  - `BugReporting/BugService.asmx` → `BugService` — **stub, TODO in code**
  - `Charts/chartservice.asmx` → `ChartService` — chart data for stats

**Key Infrastructure Files:**
- `App_Code/Code/ServerSettings.cs` — all config from appSettings (DSN, paths, throttle limits, MOTD)
- `App_Code/Code/Throttle.cs` — in-memory rate limiter using ASP.NET Cache for TTL
- `App_Code/Code/NonPageServices.cs` — singleton background timer calling `TerrariumAggregate` sproc every ~10 min
- `App_Code/Code/WordFilter.cs` — content moderation against `invalidwordlist.txt`
- `App_Code/UsageReporting.cs` — per-user/per-team usage analytics with SQL + caching

**Database Schema (SQL Server, db name `TerrariumWhidbey`):**
- Core tables: `Species`, `Peers`, `History`, `DailyPopulation`, `NodeLastContact`, `ShutdownPeers`, `TimedOutNodes`
- Auth/user tables: `UserRegister`, `VersionedSettings`
- Support tables: `Watson`, `Usage`, `UsageSummary`, `Downloads`, `RandomTips`, `Pum`, `PumTeam`
- ~17 stored procedures referenced in code (TerrariumRegisterUser, TerrariumInsertSpecies, TerrariumAggregate, etc.)
- All data access is direct ADO.NET with `SqlConnection`/`SqlCommand` — no ORM

**Key Observation:** The MVC server was started as a modernization effort but never received any game logic. ALL ecosystem functionality lives in the legacy ASMX services. Any modernization work must port these 7 services to MVC controllers.

📌 Team update (2026-02-10): ClientWPF is empty scaffolding — Client/ is the source of truth — decided by Heisenberg, Jesse, Mike
📌 Team update (2026-02-10): Build must be green before new tests — decided by Hank
📌 Team update (2026-02-10): .NET 10 modernization sprint plan created — 14 sprints, WPF on .NET 10, Silk.NET OpenGL, gRPC P2P, Dapper+stored procs, process isolation, xUnit, System.Text.Json — decided by Heisenberg
📌 Team update (2026-02-11): Beth's voice is fearless, developer-first, community-driven — decided by bradygaster

📌 Team update (2026-02-11): Diagram standards — Mermaid only, no ASCII art. All diagrams must use Mermaid — decided by Badger, bradygaster
📌 Team update (2026-02-11): VB.NET respectful framing — never refer to VB.NET negatively, just "we're C# now" — decided by bradygaster
📌 Team update (2026-02-11): Orleans + SignalR hybrid — SpeciesRegistryGrain and PopulationGrain implementation assigned to Gus in Sprint 7 — decided by Heisenberg
📌 Team update (2026-02-11): CI pipeline created (.github/workflows/build.yml) targeting src/Terrarium.sln — decided by Hank
📌 Team update (2026-02-11): CSS token naming: --glass-{category}-{element}-{modifier}, BEM for components — decided by Jesse
📌 Team update (2025-07-16): Solution uses classic .sln format (not .slnx); CS1591 suppressed during initial port — decided by Heisenberg
📌 Team update (2025-07-15): CSS tokens use `--glass-{category}-{element}-{modifier}` naming; BEM classes; `glass-theme.css` is single source of truth — decided by Jesse
