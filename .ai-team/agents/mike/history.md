# Project Context

- **Owner:** bradygaster (bradyg@microsoft.com)
- **Project:** .NET Terrarium 2.0 — peer-to-peer networked creature ecosystem game
- **Stack:** C#, .NET, WPF, ASP.NET MVC, DirectX, P2P networking
- **Created:** 2026-02-10

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2026-02-10 — Initial Domain Exploration

**Critical finding:** The `ClientWPF/` projects (Game, OrganismBase, HttpListener, Services, AsmCheck, Configuration) are **empty shells** — stub .csproj files with only `AssemblyInfo.cs`. All real source code lives under `Client/` in the matching subdirectories. Any future work must target `Client/`, not `ClientWPF/`.

**Engine Architecture:**
- `Client/Game/Classes/Engine/GameEngine.cs` — Singleton simulation engine (`GameEngine.Current`), 10-phase tick loop via `ProcessTurn()`. Phases 0–4 give organisms CPU time; 5–9 resolve actions, physics, energy, teleportation.
- `Client/Game/Classes/Engine/WorldState.cs` — Immutable world snapshots with cell-based spatial grid index. Duplicated mutable each tick, locked immutable at end.
- `Client/Game/Classes/Engine/TickActions.cs` — Aggregates organism actions per tick into Hashtable maps (Move, Attack, Eat, Defend, Reproduce).
- `Client/Game/Classes/Engine/PopulationData.cs` — Reports population to server every 600 ticks via async SOAP calls.

**Organism/Creature System:**
- `Client/OrganismBase/Classes/Creature/Organism.cs` — Abstract base. `Animal.cs` adds movement, combat, scanning. Users subclass `Animal` and handle events (Born, Idle, MoveCompleted, etc.).
- `Client/OrganismBase/Classes/Creature/Attributes/` — Point-based genetic traits declared as .NET attributes (speed, eyesight, attack, defense, eating speed, camouflage, etc., each 1–100 points).
- `Client/OrganismBase/Classes/State/` — Per-tick immutable state snapshots: `OrganismState`, `AnimalState`, `PlantState`.
- `Client/OrganismBase/Classes/Actions/` — Queued action classes: MoveToAction, AttackAction, DefendAction, EatAction, ReproduceAction.

**P2P Networking:**
- `Client/HttpListener/` — Custom async TCP socket server (`HttpWebListener`, port 50000) with namespace-based request routing (`HttpNamespaceManager`). Not using System.Net.HttpListener — this is a from-scratch implementation.
- `Client/Game/Classes/PeerToPeer/NetworkEngine.cs` — Central P2P hub. Peer discovery via SOAP `PeerDiscoveryService` every 5 minutes. Teleportation via multi-step HTTP protocol (version check → assembly check → assembly transfer → creature state transfer).
- `Client/Game/Classes/PeerToPeer/PeerManager.cs` — Good/bad peer lists, 30-second receive throttle, 1-hour bad-peer timeout, max 30 bad peers.
- `Client/Game/Classes/PeerToPeer/TeleportWorkItem.cs` — Async 4-step teleport protocol implementation.

**Security Model (3 layers):**
1. `Client/AsmCheck/asmcheck.cpp` — Native C++ IL validator. Checks every opcode, blocks P/Invoke, unsafe IL, static constructors/fields, unmanaged code, internal class access.
2. `Client/Game/Classes/Hosting/SecurityUtils.cs` — CAS policy: organisms get Execution permission only. Terrarium-signed assemblies get full trust.
3. `Client/Game/Classes/Hosting/GameScheduler.cs` — Runtime sandbox: time-sliced execution with deadlock detection (5-second kernel time threshold → permanent blacklist).

**Assembly Management:**
- `Client/Game/Classes/Engine/PrivateAssemblyCache.cs` — Obfuscated directory storage, custom `AssemblyResolve` hook, blacklist via zero-length marker files, crash detection via `LastRun` tracking.

**Configuration:**
- `Client/Configuration/Classes/Config/GameConfig.cs` — Static config class, XML-backed boolean/string settings.
- `Client/Configuration/Classes/Tools/` — ErrorLog (thread-safe, Mutex), Profiler (TRACE-mode), TimeMonitor (QueryPerformanceCounter).

**Services:**
- `Client/Services/Web References/` — 8 SOAP proxies: Discovery, Messaging, Species, Reporting, Charts, BugReporting, Watson, Usage.

📌 Team update (2026-02-10): MVC Server is a scaffold — all game logic lives in legacy ASMX — decided by Gus
📌 Team update (2026-02-10): Build must be green before new tests — decided by Hank
📌 Team update (2026-02-10): .NET 10 modernization sprint plan created — 14 sprints, WPF on .NET 10, Silk.NET OpenGL, gRPC P2P, Dapper+stored procs, process isolation, xUnit, System.Text.Json — decided by Heisenberg
📌 Team update (2026-02-11): Beth's voice is fearless, developer-first, community-driven — decided by bradygaster

📌 Team update (2026-02-11): Diagram standards — Mermaid only, no ASCII art. All diagrams must use Mermaid — decided by Badger, bradygaster
📌 Team update (2026-02-11): VB.NET respectful framing — never refer to VB.NET negatively, just "we're C# now" — decided by bradygaster
📌 Team update (2026-02-11): Orleans + SignalR hybrid — EcosystemGrain tick loop, PeerGrain leases, SpeciesRegistryGrain CRUD, PopulationGrain write-behind. Sprint 7 grain implementation — decided by Heisenberg
📌 Team update (2026-02-11): CI pipeline created (.github/workflows/build.yml) targeting src/Terrarium.sln — decided by Hank
📌 Team update (2026-02-11): Aspire packages pinned — Aspire 13.1.0, ServiceDiscovery/Resilience 10.3.0, OpenTelemetry 1.15.0 — decided by Saul
📌 Team update (2026-02-11): CSS token naming: --glass-{category}-{element}-{modifier}, BEM for components — decided by Jesse
📌 Team update (2026-02-11): SignalR Hub contract layer locked for Sprint 7 (ITerrariumHub, ITerrariumClient, CreatureTeleport, PeerAnnounce, EcosystemTick, error handling via ReceiveError callback) — decided by Mike
📌 Team update (2026-02-11): Services layer is pure class lib (no ServiceDefaults), interface-first design, consumers configure Aspire integration at DI level — decided by Mike
📌 Team update (2026-02-11): Keep ArrayList.Scan() for now (deferred to Game project port); will change to List<OrganismState> when Game is ported — decided by Mike
📌 Team update (2026-02-11): Terrarium.Web Blazor Interactive Server created (PR #118), component library built on Glass CSS, Canvas via ElementReference, ready for Sprint 4 engine integration — decided by Skyler
📌 Team update (2026-02-11): Glass CSS expanded to 60+ new tokens, all 76 original image assets cataloged and extracted to wwwroot/assets/, manifest.json created — decided by Jesse
📌 Team update (2026-02-11): Organism Isolation architecture (3 layers: CreatureValidator static check, OrganismSandbox AssemblyLoadContext, OrganismHost timeout+safety) replaces AppDomain sandbox — decided by Heisenberg
📌 Team update (2026-02-11): Hub-and-spoke SignalR architecture finalized: 8 hub methods, 7 client callbacks, error struct, rate limiting per connection, heartbeat/lease model, reconnect=rejoin, 512KB message size limit — decided by Heisenberg
📌 Team update (2026-02-11): Server.Tests xUnit project created (PR #13), 17 tests (ServerHealth, MessagingEndpoints, Throttle), 4 passing now, 13 awaiting Gus bootstrap — decided by Hank
📌 Team update (2026-02-11): SDK samples structure finalized (src/Terrarium.Samples/{Name}/ standalone projects), SimpleHerbivore/SimpleCarnivore/SimplePlant ported, README documents creature authoring — decided by Hank
📌 Team update (2026-02-11): Species & Reporting endpoints: assembly storage deferred, word filter deferred, chart endpoints under /api/reporting/stats/, ReportPopulation returns Success on server error — decided by Gus
📌 Team update (2026-02-11): Terrarium.Server created (PR #112), Minimal APIs, ServerSettings via IOptions, messaging endpoints, throttle via IMemoryCache — decided by Gus
📌 Team update (2026-02-11): Road ahead blog post written (sprint-prep-the-road-ahead.md): 48 issues, 7 sprints, 89 minutes wall-clock parallelism, per-agent workload, emotional frame — decided by Beth

### 2025-07-16 — Terrarium.Services Client-Side Service Layer (#14)

Created `src/Terrarium.Services/` — a clean HttpClient-based replacement for the 8 legacy ASMX Web Reference proxies in `Client/Services/Web References/`.

**What was built:**
- 6 interfaces: IMessagingService, IPeerDiscoveryService, ISpeciesService, IPopulationService, IReportingService, IChartService
- 6 HttpClient-based implementations in `Clients/`
- 10 model types in `Models/` (BugReport, UsageData, SpeciesInfo, PeerInfo, PopulationData, PopulationEntry, VersionCheckResult + 3 enums)
- `ServiceCollectionExtensions` with DI registration and Aspire service discovery support
- Added to `Terrarium.sln`, builds clean

**Legacy proxy → new service mapping:**
- Messaging → IMessagingService (aligned with Gus's `/api/messaging/*` endpoints)
- Discovery → IPeerDiscoveryService
- Species → ISpeciesService
- Reporting → IPopulationService
- BugReporting + Watson + Usage → IReportingService (consolidated)
- Charts → IChartService

**Key decisions:**
- Interface-first design for testability and DI
- No ServiceDefaults dependency — pure class library, depends only on `Microsoft.Extensions.Http` and `Microsoft.Extensions.Options`
- System.Text.Json serialization, no DataSet, no SOAP
- CancellationToken on all async methods
- Aspire service discovery via `AddTerrariumServices(serviceName)` overload

PR #109, branch `squad/14-services-layer`.

### 2025-07-16 — Terrarium.Net SignalR Hub Layer (#16)

Created `src/Terrarium.Net/` — the SignalR contract layer replacing the legacy custom HttpWebListener and TCP peer-to-peer networking stack.

**What was built:**
- `ITerrariumClient` — server-to-client interface: `ReceiveEcosystemTick`, `ReceiveWorldStateUpdate`, `ReceiveCreatureTeleport`, `ReceivePeerAnnounce`
- `ITerrariumHub` — client-to-server interface: `JoinEcosystem`, `LeaveEcosystem`, `TeleportCreature`, `AnnouncePeer`, `RequestWorldState`
- `TerrariumHub : Hub<ITerrariumClient>` — thin relay implementation using SignalR groups per ecosystem
- 4 message types in `Messages/`:
  - `WorldStateUpdate` — tick-indexed world snapshot
  - `CreatureTeleport` — replaces legacy 4-step HTTP teleport protocol (version check → assembly check → assembly transfer → state transfer) with a single message carrying optional assembly payload
  - `PeerAnnounce` — replaces SOAP-based PeerDiscoveryService polling with push-based join/leave/heartbeat
  - `EcosystemTick` — lightweight tick clock signal

**Legacy → new mapping:**
- `HttpWebListener` (custom TCP, port 50000) → SignalR WebSocket transport
- `NetworkEngine.Teleport()` 4-step HTTP → `CreatureTeleport` single message
- `PeerDiscoveryService` SOAP polling → `PeerAnnounce` + SignalR groups
- `PeerManager` good/bad peer lists → Orleans `PeerGrain` (Sprint 7)

**Key decisions:**
- Uses `FrameworkReference Microsoft.AspNetCore.App` — SignalR is in-box for net10.0, no NuGet package needed
- Hub is intentionally thin — all TODO comments mark where Orleans grain calls will be added in Sprint 7
- `CreatureTeleport.AssemblyPayload` is optional Base64 — only sent when target peer lacks the assembly
- `PeerAction` enum (Join/Leave/Heartbeat) replaces the lease-timeout pattern from `PeerManager`
- No dependency on other Terrarium projects — pure contract library

PR #113, branch `squad/16-networking-layer`.

### 2025-07-16 — Sprint 7 Networking Implementation (#50, #52, #54)

**Issue #50 — TerrariumHub SignalR Hub (full implementation):**
- Rewrote `TerrariumHub` in `src/Terrarium.Net/TerrariumHub.cs` with in-memory `ConcurrentDictionary<string, PeerState>` for peer tracking
- Per-connection rate limiting with sliding window: TeleportCreature 10/60s, ReportPopulation 2/60s, RequestWorldState 5/60s, RequestPeerList 2/60s, Heartbeat 3/60s
- Hub never throws — all errors go through `ReceiveError` callback per Heisenberg's design
- `OnDisconnectedAsync` cleans up peer state and broadcasts `PeerAction.Leave`
- Teleportation routes to specific peer or random peer in ecosystem (excludes sender)
- SignalR groups use `ecosystem-{id}` naming convention
- Wired into Server `Program.cs`: `AddSignalR()` with 512KB max message size, `MapHub<TerrariumHub>("/hubs/terrarium")`
- Added `Terrarium.Net` project reference to `Terrarium.Server.csproj`
- TODO comments mark Orleans grain integration points for Sprint 11

**Issue #52 — NetworkEngine Port:**
- Created `src/Terrarium.Game/Networking/NetworkEngine.cs` — client-side SignalR connection management
- Replaces legacy `Client/Game/Classes/PeerToPeer/NetworkEngine.cs` (custom TCP, port 50000)
- `Channel<T>` bounded work queue (capacity 256, drop-oldest) for outbound operations
- 30-second throttle on teleport and population report enqueue (matches legacy `PeerManager`)
- Bad-peer blacklist with configurable timeout (default 1 hour, matches legacy 1-hour bad-peer timeout)
- Automatic reconnect with escalating delays (0s → 2s → 10s → 30s → 60s), auto-rejoin ecosystem on reconnect
- 30-second heartbeat via `PeriodicTimer`, with blacklist cleanup on each tick
- Event-based API: `OnCreatureTeleported`, `OnPeerAnnounced`, `OnPeerListReceived`, etc.
- Added `Microsoft.AspNetCore.SignalR.Client` package and `Terrarium.Net` reference to `Terrarium.Game.csproj`

**Issue #54 — Server-to-Server gRPC (future-proofing):**
- Created `src/Terrarium.Grpc/` — code-first gRPC using `protobuf-net.Grpc` (no .proto files needed)
- `IServerSyncService` contract: `TransferCreatureAsync`, `SyncPopulationAsync`, `HeartbeatAsync`, `ListEcosystemsAsync`
- 8 DataContract message types with explicit `DataMember` ordering for wire stability
- Added to `Terrarium.sln`
- This is contract-only — implementation comes when multi-server scaling is needed

**Key decisions:**
- In-memory state via `ConcurrentDictionary` for Sprint 7 — Orleans grain calls replace this in Sprint 11
- Code-first gRPC over .proto files — .NET-to-.NET communication doesn't need language-neutral schemas
- `Channel<T>` over `BlockingCollection` — modern async, backpressure-aware, single-reader optimized
- SignalR hub endpoint at `/hubs/terrarium` — follows ASP.NET Core convention

### 2025-07-16 — Sprint 9 Engine Wiring (#63, #64, #65)

**Issue #63 — Wire Game Engine to Renderer:**
- Created `src/Terrarium.Game/Rendering/IEngineRenderer.cs` — engine-side renderer abstraction (no dependency on Terrarium.Web)
- `IEngineRenderer.RenderWorldAsync(WorldRenderData)` — single method, called once per completed tick
- `WorldRenderData` — complete render payload: organisms (position, skin family, energy, action), teleport zones, world dimensions
- `OrganismRenderData` — per-creature: ID, species name, skin family, position, radius, energy, alive state, DisplayAction
- `TeleportZoneRenderData` — teleporter position/size for visual rendering
- Created `src/Terrarium.Game/Rendering/GameRenderBridge.cs` — converts WorldState → WorldRenderData, dispatches to IEngineRenderer
- Uses `ZOrderedOrganisms` for proper draw ordering, extracts skin family from AnimalSpecies/PlantSpecies enums
- Wired into `GameEngine.ProcessTurn()` — fire-and-forget render dispatch after each completed tick (phase 9 → phase 0 reset)

**Issue #64 — Wire Game Engine to SignalR Networking:**
- Created `src/Terrarium.Game/Networking/GameNetworkBridge.cs` — bridges GameEngine ↔ NetworkEngine (INetworkEngine)
- Outbound: `SendTeleport()` serializes OrganismState as JSON `TeleportStatePayload`, enqueues via NetworkEngine
- Inbound: `HandleInboundTeleportAsync` receives CreatureTeleport, queues into GameEngine's `_newOrganismQueue` via `OnTeleportReceived()`
- Peer tracking: `HandlePeerAnnouncedAsync` fires EngineStateChanged events on join/leave
- Wired teleportation in `GameEngine.TeleportOrganisms()` — organisms in teleport zones now call `_networkBridge.SendTeleport()`
- Added `InsertOrganismsFromQueue` handling for `CreatureTeleport` objects (dequeue and log — full materialization pending scheduler/assembly loading)
- Made `OnEngineStateChanged` internal so bridge can notify UI of peer events

**Issue #65 — Wire Game Engine to Server Services:**
- Created `src/Terrarium.Game/Services/GameServiceBridge.cs` — bridges GameEngine to Terrarium.Services HTTP layer
- `ReportPopulationAsync()` — converts species populations dict → `PopulationHistoryRow` list, calls IPopulationService
- `RegisterSpeciesAsync()` — registers new species via ISpeciesService with assembly metadata
- `ReportErrorAsync()` — Watson-style crash data via IWatsonService (RuntimeInformation for OS/CLR/version)
- `ReportBugAsync()` — bug reporting via IReportingService
- Wired `PopulationData.EndTick()` to fire-and-forget `ServiceBridge.ReportPopulationAsync()` every 600 ticks
- Added `Terrarium.Services` project reference to `Terrarium.Game.csproj`
- Added bridge properties to `IGameEngine` interface and `GameEngine` class
- Registered `GameRenderBridge` and `GameServiceBridge` in `GameServiceExtensions.AddTerrariumGameEngine()`

**Key decisions:**
- Engine-side `IEngineRenderer` abstraction (not `IGameRenderer` from Terrarium.Web) — keeps Game project free of ASP.NET/Blazor/JS dependencies
- Fire-and-forget for render and population report dispatches — game loop must never block on I/O
- `GameNetworkBridge` takes `INetworkEngine` (interface), not `NetworkEngine` (class) — DI/testability
- `TeleportStatePayload` custom JSON type (not raw OrganismState) — clean serialization boundary, no internal state leakage
- `GameServiceBridge` wraps all server calls in try/catch — server failures never crash the game loop

### 2025-01-22 — Sprint 10 Creature Introduction Pipeline (#69)

**What was built:**
- Fixed pre-existing server build errors in `SpeciesEndpoints.cs` (missing parameters on `/extinct` endpoint)
- Added `GET /{name}/assembly` endpoint to download species assemblies by name and version
- Enhanced `POST /register` endpoint to save uploaded assemblies to disk at configured `AssemblyPath`
- Created `GameEngine.IntroduceCreatureFromPac()` — loads creature from PrivateAssemblyCache, validates, extracts species, adds to game
- Created `GameEngine.IntroduceCreatureFromServerAsync()` — downloads assembly from server, validates, saves to PAC, introduces into game
- Added `GameServiceBridge.GetSpeciesAssemblyAsync()` — client-side method to download species assembly bytes
- Updated `IGameEngine` interface with the two new introduction methods
- Fixed Upload.razor drag-and-drop compilation error (removed unsupported DnD in Blazor Server)

**Key implementation details:**
- Server stores assemblies at `ServerSettings.AssemblyPath` (configured in appsettings.json)
- Assembly validation runs at both upload (server) and download (client) points via `AssemblyValidator`
- Uses existing `Species.GetSpeciesFromAssembly()` pattern for extracting species metadata
- Auto-registers newly introduced creatures with server if `ServiceBridge` is configured
- Full async download pipeline with temp file validation before committing to PAC
- Synchronous PAC introduction path for already-cached assemblies

**File locations:**
- Server endpoints: `src/Terrarium.Server/SpeciesEndpoints.cs`
- Game engine methods: `src/Terrarium.Game/GameEngine.cs` (lines ~291-450)
- Service bridge: `src/Terrarium.Game/Services/GameServiceBridge.cs`
- Interface: `src/Terrarium.Game/IGameEngine.cs`
- Client service: `src/Terrarium.Services/Clients/SpeciesServiceClient.cs` (already had `GetSpeciesAssemblyAsync`)

**Build verified:** Full solution compiles with zero errors.
