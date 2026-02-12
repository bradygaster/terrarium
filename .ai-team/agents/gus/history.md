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
📌 Team update (2026-02-11): Aspire packages pinned — Aspire 13.1.0, ServiceDiscovery/Resilience 10.3.0, OpenTelemetry 1.15.0 — decided by Saul
📌 Team update (2026-02-11): CSS token naming: --glass-{category}-{element}-{modifier}, BEM for components — decided by Jesse
📌 Team update (2026-02-11): Services layer interface-first design (no ServiceDefaults dependency), consumers configure Aspire at DI level — decided by Mike
📌 Team update (2026-02-11): SignalR Hub contract locked: ITerrariumHub (8 methods), ITerrariumClient (7 callbacks), error struct via ReceiveError callback — decided by Mike
📌 Team update (2026-02-11): Terrarium.Web Blazor Interactive Server created (PR #118), SignalR-ready components, Glass CSS integrated — decided by Skyler
📌 Team update (2026-02-11): Glass CSS expanded to 60+ tokens, all 76 original assets cataloged in wwwroot/assets/ — decided by Jesse
📌 Team update (2026-02-11): Server.Tests xUnit integration tests created (17 tests, 4 passing, 13 pending server bootstrap) — decided by Hank
📌 Team update (2026-02-11): Species & Reporting endpoints: assembly storage deferred, word filter deferred, /api/reporting/stats/ consolidation — decided by Gus

### 2025-07-16 — Sprint 1: Terrarium.Server Bootstrap (#9, #10, #11)

**What was done:**
- Created `src/Terrarium.Server/` as a Minimal API host on net10.0
- Added Dapper + Microsoft.Data.SqlClient NuGet packages for future SQL access
- Ported `ServerSettings.cs` from static `ConfigurationManager.AppSettings` to `IOptions<ServerSettings>` pattern bound to `"Terrarium"` config section
- Ported `Messaging.asmx.cs` (SOAP WebMethods) to three JSON endpoints: `GET /api/messaging/welcome`, `/motd`, `/version`
- Ported `Throttle.cs` from ASP.NET `HttpContext.Current.Cache` + `Hashtable` + `lock(typeof(Throttle))` to `IMemoryCache` + `ConcurrentDictionary` + `Interlocked` operations
- Registered server as Aspire resource in AppHost (`builder.AddProject<Projects.Terrarium_Server>("server")`)
- Wired ServiceDefaults (OpenTelemetry, health checks, service discovery)
- PR #112 → `squadified`

**Key decisions:**
- ThrottleService is a singleton; ThrottleMiddleware is transient (per-request)
- Default rate limit: 60 requests/IP/minute — matches legacy server's typical throttle window
- Messaging endpoints return `{ "message": "..." }` / `{ "version": "..." }` JSON — simple, predictable shape
- No database calls yet — Dapper is referenced but not invoked until discovery/species endpoints are ported

### 2025-07-16 — Sprint 2: Species & Reporting Endpoints (#26, #27)

**What was done:**
- Created `src/Terrarium.Server/SpeciesEndpoints.cs` — 5 endpoints porting the legacy `SpeciesService` ASMX
- Created `src/Terrarium.Server/ReportingEndpoints.cs` — 4 endpoints porting `ReportingService` + `ChartService`/`ChartBuilder`
- Wired both endpoint groups in `Program.cs`
- PR #120 → `squadified`

**Key decisions:**
- Assembly file I/O omitted — legacy CAS-based `FileIOPermission` doesn't apply in .NET 10
- Word filter (PoliCheck) omitted — depends on static file; can be added later
- `ReportPopulation` returns `Success` on error (matching legacy behavior to prevent retry storms)
- Chart endpoints consolidated under `/api/reporting/stats/` rather than separate `/api/charts/`
- `SpeciesServiceStatus` and `ReportingReturnCode` enums ported as-is for client compatibility

### 2026-02-11 — Sprint 12: Server Monitoring & Observability (#84)

**What was done:**
- Created `src/Terrarium.Server/HealthChecks/` with three custom IHealthCheck implementations:
  - `DatabaseHealthCheck` — placeholder for future DB connectivity check (returns Degraded until implemented)
  - `SignalRHubHealthCheck` — verifies SignalR hub context is accessible
  - `AssemblyCacheHealthCheck` — checks assembly cache disk space with configurable thresholds (100MB warning, 10MB unhealthy)
- Created `TerrariumMetrics` class in `Terrarium.ServiceDefaults/` with System.Diagnostics.Metrics:
  - `ConnectedPeerCount` (ObservableGauge) — current connected peers across all ecosystems
  - `ActiveSpeciesCount` (ObservableGauge) — active species with non-zero population
  - `SignalRConnections` (ObservableGauge) — active SignalR connections (same as peer count)
  - `PopulationReportsReceived` (Counter) — total population reports processed
  - `TeleportationEvents` (Counter) — total teleportation events processed
  - `AssemblyUploads` (Counter) — total assembly uploads (future sprint)
- Enhanced structured logging in `TerrariumHub.cs`:
  - Added log scopes with structured properties: `PeerId`, `EcosystemId`, `TickNumber`, `TeleportId`, `OrganismId`
  - Updated `JoinEcosystem`, `LeaveEcosystem`, `ReportPopulation`, `TeleportCreature`, `OnDisconnectedAsync`
  - Consistent log levels: Information for normal ops, Warning for degraded, Error for failures
- Enhanced structured logging in `PopulationTrackingService.cs`:
  - Added log scopes for `RecordReport`, `RemovePeer`, `CleanupStaleEcosystems`
- Wired metrics in `Program.cs` with lambda providers for peer/species counts
- Wired health checks in `Program.cs` with tags: `["ready"]` for all custom checks, `["live"]` for self-check
- Added `System.Diagnostics.Metrics` using to `Program.cs` for `IMeterFactory`
- Added project reference from `Terrarium.Net` to `Terrarium.ServiceDefaults` to access `TerrariumMetrics`
- Updated `IPopulationTrackingService` with `GetActiveSpeciesCount()` method for metrics
- Added static `GetConnectedPeerCount()` method to `TerrariumHub` for metrics
- All 4 ServerHealthTests passing: `/health`, `/alive`, root endpoint, Terrarium Server response

**Key decisions:**
- Health checks use Aspire-integrated pattern with tags for liveness vs readiness
- Metrics use `System.Diagnostics.Metrics` (native .NET) rather than custom telemetry
- Structured logging uses ILogger.BeginScope() with Dictionary<string, object> for consistent property names
- DatabaseHealthCheck returns Degraded (not Unhealthy) until database layer is implemented
- TerrariumMetrics lives in ServiceDefaults (shared) so both Server and Net projects can reference it
- Assembly uploads counter exists but not yet wired (pending Sprint 13 when assembly storage is implemented)
- Log properties renamed from generic names to semantic names: `ConnectionId` → `PeerId`, `Total` → `TotalOrganisms`
- Metrics use dimensional tags (e.g., `ecosystem_id`) for filtering in Aspire dashboard

### 2025-07-17 — Server Connectivity Diagnosis

**Context:** Web frontend at :5190 shows "network status: red", empty blue canvas, zero traffic to server at :5180.

**Findings:**

1. **Hub path mismatch:** Server maps `TerrariumHub` at `/hubs/terrarium` (`Program.cs:66`), but `TerrariumHubClient.cs:52` connects to `/terrarium`. SignalR negotiate would 404.
2. **No CORS configured:** Server has zero CORS setup — no `AddCors()`, no `UseCors()`. Not blocking for Blazor Server (server-side hub client), but needed for any browser-direct SignalR.
3. **Hub connection never started:** `TerrariumHubClient` is registered as singleton and callbacks are wired in `Home.razor`, but `StartAsync()` is never called anywhere. The connection is built but never opened — primary cause of zero traffic.
4. **Aspire service discovery: correct.** Server registered as `"server"` in AppHost, web resolves via `Services:server:http:0` config key.
5. **API endpoints: correct.** All 8 endpoint groups match what `Terrarium.Services` clients expect.
6. **No auth middleware:** No `UseAuthentication`/`UseAuthorization` — unauthenticated SignalR is fine.
7. **ThrottleMiddleware** runs on all requests including WebSocket upgrades — 60 req/min/IP, minor concern for rapid reconnect cycles.

**Key file paths:**
- `src/Terrarium.Server/Program.cs` — server startup, hub mapping at line 66, endpoint groups lines 68-91
- `src/Terrarium.Server/Middleware/ThrottleMiddleware.cs` — global rate limiter, 60 req/min/IP
- `src/Terrarium.Net/TerrariumHub.cs` — hub implementation with in-memory peer state
- `src/Terrarium.Net/ITerrariumHub.cs` — 8 server methods (contract)
- `src/Terrarium.Net/ITerrariumClient.cs` — 7 client callbacks (contract)
- `src/Terrarium.Web/Services/TerrariumHubClient.cs` — SignalR client, connects to `{serverUrl}/terrarium`
- `src/Terrarium.Web/Program.cs` — web startup, registers hub client singleton at line 31
- `src/Terrarium.Services/ServiceCollectionExtensions.cs` — typed HttpClient registrations for all API services
- `src/Terrarium.AppHost/Program.cs` — Aspire orchestration, server="server", web="web"
- `src/Terrarium.Server/Properties/launchSettings.json` — server on port 5180
- `src/Terrarium.Web/Properties/launchSettings.json` — web on port 5190

### 2025-07-17 — CORS Configuration for SignalR

**What was done:**
- Confirmed hub path: `TerrariumHub` is mapped at `/hubs/terrarium` in `Program.cs:84` — no change needed, already matches canonical path
- Added `AddCors()` with a default policy allowing localhost (any port), 127.0.0.1, and `*.internal` origins
- Policy allows GET + POST methods (SignalR requirements), all headers, and credentials
- Added `app.UseCors()` in the middleware pipeline before endpoint mapping
- Build verified: 0 warnings, 0 errors

**Key decisions:**
- Used `SetIsOriginAllowed` with host check rather than hardcoded origin list — handles any localhost port and Aspire's `.internal` service discovery URLs
- Default policy (no named policy) keeps it simple — one CORS config for the whole server
- `AllowCredentials()` required for SignalR's negotiate handshake
- Placed `UseCors()` after `UseMiddleware<ThrottleMiddleware>()` and before `MapHub`/`MapGet` — correct middleware ordering

### 2025-07-17 — Server Logging Audit & Missing Game Loop Diagnosis

**Context:** Frontend shows "Connected" to SignalR hub but canvas is blank, zero logs from any component.

**What was done:**
- Added `OnConnectedAsync` to `TerrariumHub` — logs every client connection with `ConnectionId`
- Added startup banner log in `Program.cs`: "Terrarium Server started — hub mapped at /hubs/terrarium"
- Added heartbeat log to `NonPageServicesWorker` maintenance loop cycle
- Build verified: 0 warnings, 0 errors

**Root cause of blank canvas — NO GAME LOOP EXISTS:**
- `ITerrariumClient` defines `ReceiveEcosystemTick` and `ReceiveWorldStateUpdate` callbacks
- `IEcosystemNotifier` defines `NotifyTickAsync` and `NotifyWorldStateAsync` methods
- **Nothing on the server implements or calls these.** No `BackgroundService` runs a simulation tick or broadcasts world state.
- `RequestWorldState` returns `WorldWidth: 0, WorldHeight: 0, TickNumber: 0` — the TODO says "Sprint 11 — fetch from EcosystemGrain" but nothing fills this gap in the meantime.
- The server is a **passive relay** — it only sends data when clients push first. No client pushes because there's no game simulation.

**Key finding:** The server needs a game loop `BackgroundService` that:
1. Maintains world state (terrain, dimensions, organisms)
2. Runs periodic ticks
3. Broadcasts `EcosystemTick` + `WorldStateUpdate` via `IHubContext<TerrariumHub, ITerrariumClient>`

**Files modified:**
- `src/Terrarium.Net/TerrariumHub.cs` — added `OnConnectedAsync`
- `src/Terrarium.Server/Program.cs` — added startup banner log
- `src/Terrarium.Server/Workers/NonPageServicesWorker.cs` — added heartbeat log
