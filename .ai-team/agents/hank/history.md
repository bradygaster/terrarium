# Project Context

- **Owner:** bradygaster (bradyg@microsoft.com)
- **Project:** .NET Terrarium 2.0 — peer-to-peer networked creature ecosystem game
- **Stack:** C#, .NET, WPF, ASP.NET MVC, DirectX, P2P networking
- **Created:** 2026-02-10

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2025-01-XX — Initial Exploration Pass

**Test Framework & Patterns:**
- Test project: `ServerMVC/TerrariumServer.Tests/` — MSTest (VS Unit Test Framework v10.0), .NET Framework 4.0
- 2 test files: `Controllers/HomeControllerTest.cs` (2 tests), `Controllers/AccountControllerTest.cs` (13 tests)
- All 15 tests are stock ASP.NET MVC 2 template tests — zero Terrarium domain logic tested
- Uses hand-rolled mocks (MockFormsAuthenticationService, MockMembershipService, MockHttpContext)
- Arrange/Act/Assert pattern throughout

**Build Results:**
- `dotnet build Terrraium2010.sln` → FAILED, 15 errors
- MSB3644: Missing .NET Framework 4.0 targeting pack (all projects)
- MSB4278: Missing VS 2010 Web Application targets (ServerMVC/TerrariumServer)
- Solution filename has a typo: `Terrraium2010.sln` (three R's)
- No CI/CD exists — zero automation, no YAML pipelines, no build scripts

**Key File Paths:**
- Test project: `ServerMVC/TerrariumServer.Tests/TerrariumServer.Tests.csproj`
- Main solution: `Terrraium2010.sln` (VS 2010 format, 15 projects)
- Legacy client solution: `Client/client.sln` (VS 2008 format, 13 projects)
- Client version info: `Client/VersionInfo.cs` (v2.1.0.*)
- SDK docs: `SDK/Docs/` (4 files: .doc + .chm)
- SDK tutorials: `SDK/Manuals/` (CS + VB tutorials)
- SDK skeletons: `SDK/Skeletons/` (4 files: CS + VB, Carnivore + Herbivore)
- SDK exercises: `SDK/Solutions/CS/Exercise{1,2,3}/` and `SDK/Solutions/VB/`
- Samples: `Samples/` (Herbivore, Carnivore, Plant — VS 2005 .sln)
- Tools: `Tools/ServerConfig/` (IIS setup wizard, SQL schema), `Tools/StyleEditor/` (Metal UI framework)
- SQL schema: `Tools/ServerConfig/Setup/SqlSetup.sql`
- Findings written to: `SDK/README.md`

📌 Team update (2026-02-10): ClientWPF is empty scaffolding — Client/ is the source of truth — decided by Heisenberg, Jesse, Mike
📌 Team update (2026-02-10): MVC Server is a scaffold — all game logic lives in legacy ASMX — decided by Gus
📌 Team update (2026-02-10): .NET 10 modernization sprint plan created — 14 sprints, WPF on .NET 10, Silk.NET OpenGL, gRPC P2P, Dapper+stored procs, process isolation, xUnit, System.Text.Json — decided by Heisenberg
📌 Team update (2026-02-11): Beth's voice is fearless, developer-first, community-driven — decided by bradygaster

📌 Team update (2026-02-11): Diagram standards — Mermaid only, no ASCII art. All diagrams must use Mermaid — decided by Badger, bradygaster
📌 Team update (2026-02-11): VB.NET respectful framing — never refer to VB.NET negatively, just "we're C# now" — decided by bradygaster
📌 Team update (2026-02-11): Orleans + SignalR hybrid — TestCluster-based grain tests assigned to Hank in Sprint 7 — decided by Heisenberg
📌 Team update (2026-02-11): CSS token naming: --glass-{category}-{element}-{modifier}, BEM for components — decided by Jesse
📌 Team update (2026-02-11): Services layer interface-first (no ServiceDefaults), HttpClient-based, System.Text.Json — decided by Mike
📌 Team update (2026-02-11): SignalR Hub contract locked (8 hub methods, 7 client callbacks, ReceiveError for error handling) — decided by Mike
📌 Team update (2026-02-11): Terrarium.Web Blazor Interactive Server created (PR #118), components SignalR-ready, Glass CSS integrated — decided by Skyler
📌 Team update (2026-02-11): Glass CSS expanded to 60+ tokens, all 76 original assets cataloged and available in wwwroot/assets/ — decided by Jesse
📌 Team update (2026-02-11): Server.Tests xUnit integration tests (17 tests, 4 passing, 13 pending server bootstrap) — decided by Hank
📌 Team update (2026-02-11): SDK Samples structure (standalone .csproj per sample in src/Terrarium.Samples/), SimpleHerbivore/SimpleCarnivore/SimplePlant ported — decided by Hank
📌 Team update (2026-02-11): Species & Reporting endpoints (assembly storage deferred, word filter deferred, /api/reporting/stats/ consolidation) — decided by Gus
📌 Team update (2026-02-11): Organism Isolation architecture (3 layers: static validator, AssemblyLoadContext sandbox, execution safety host) — decided by Heisenberg
📌 Team update (2026-02-11): Hub-and-spoke SignalR architecture finalized (rate limiting, heartbeat/lease, reconnect=rejoin, error struct) — decided by Heisenberg
📌 Team update (2026-02-11): Road ahead blog post written (sprint-prep-the-road-ahead.md): 48 issues, 7 sprints, 89 minutes wall-clock — decided by Beth

### 2025-07-15 — CI Pipeline Created (Issue #5)

**What was done:**
- Created `.github/workflows/build.yml` — the first CI pipeline for the project
- Triggers on push to `main` and `squadified` branches, and on all PRs
- Ubuntu runner (cross-platform web app per Brady's decision)
- .NET 10 SDK setup with global.json detection (falls back to 10.0.x + preview quality)
- NuGet package caching keyed on csproj/Directory.Packages.props/Directory.Build.props
- Restore → Build (Release) → Test pipeline targeting `src/Terrarium.sln`
- Decided against a separate `pr-check.yml` — no label/title conventions exist yet to validate

**Key decisions:**
- Used `dotnet-quality: preview` since .NET 10 is in preview
- Solution path is `src/Terrarium.sln` (Heisenberg's new structure, not the legacy `Terrraium2010.sln`)
- Cache key includes Directory.Packages.props and Directory.Build.props for central package management support
- Single workflow handles both push CI and PR checks — no unnecessary ceremony
📌 Team update (2025-07-16): Solution uses classic .sln format (not .slnx); CS1591 suppressed during initial port — decided by Heisenberg
📌 Team update (2025-07-15): CSS tokens use `--glass-{category}-{element}-{modifier}` naming; BEM classes; `glass-theme.css` is single source of truth — decided by Jesse
📌 Team update (2026-02-10): Keep ArrayList on Scan() until Game project ported — decided by Mike
### 2026-02-11 — OrganismBase Unit Tests (#7)

**Test Project Created:** `src/Terrarium.OrganismBase.Tests/`
- xUnit on net10.0, references `Terrarium.OrganismBase` project
- **197 tests, 0 failures** across 16 test files + 1 helper file
- PR #106 → `squad/7-organismbase-tests` → `squad/3-port-organismbase`

**What's Tested:**
- Point-based attributes (AttackDamage, DefendDamage, EatingSpeed, Eyesight, MaxSpeed, MaxEnergy, Camouflage) — zero/mid/max points, validation, GetWarnings
- Non-point attributes (Carnivore, MatureSize, SeedSpread, AuthorInfo, OrganismClass, AnimalSkin, PlantSkin, MarkingColor)
- OrganismState — energy management, EnergyState buckets, UpperBoundary, BurnEnergy, tick aging, growth, position, immutability, FoodChunks, reproduction wait, CompareTo, IsAdjacentOrOverlapping
- AnimalState — damage/healing, PercentInjured, EnergyRequiredToMove, rot, IncreaseRadiusTo, CloneMutable, PreviousDisplayAction, Antennas
- PlantState — food chunks, GiveEnergy (optimal/zero light), IncreaseRadiusTo (height+food), CloneMutable, immutability
- Vector — constructor, magnitude (true + fast), direction, Scale, Rotate, GetUnitVector, Subtract, Add, ToRadians/ToDegrees
- MovementVector — speed validation, empty destination, defensive copy
- PendingActions — all 5 action setters, immutability guard
- OrganismEventResults — all 7 event properties, immutability guard
- AttackedEventArgsCollection — Add, indexer, enumerator, immutability (generic List<T> modernization verified)
- AntennaState — constructors, position encoding, AntennaValue get/set, immutability
- Growth — animal and plant grow mechanics, already-mature, low-energy, immutable guards
- EngineSettings — constant relationships, EngineSettingsAsserts()
- Exceptions — GameEngineException, TooManyPoints, SizeOutOfRange hierarchy
- GenericTypeDescriptor — ICustomTypeDescriptor implementation
- Species interfaces — IAnimalSpecies, IPlantSpecies, IsSameSpecies

**Testing Patterns Established:**
- Mock species (`MockAnimalSpecies`, `MockPlantSpecies`) in `TestHelpers.cs` — reusable across all state tests
- Many constructors are `internal` (Action, OrganismState) — tests use concrete subclasses (AnimalState, PlantState) instead
- Build command: `dotnet test src/Terrarium.OrganismBase.Tests/`

### 2026-02-11 — Server Integration Tests (#13)

**Test Project Created:** `src/Terrarium.Server.Tests/`
- xUnit on net10.0, references `Terrarium.Server` project
- Uses `Microsoft.AspNetCore.Mvc.Testing` with `WebApplicationFactory<Program>`
- **17 tests total: 4 passing, 13 awaiting Gus's server implementation**
- PR → `squad/13-server-tests` → `squadified`

**What's Tested:**
- ServerHealthTests (4 tests):
  - `Server_Starts_And_Responds` ✅ — root endpoint returns success
  - `Root_Returns_Terrarium_Server` ✅ — verifies exact response text
  - `Health_Endpoint_Returns_Healthy` ⏳ — awaits ServiceDefaults/MapDefaultEndpoints
  - `Alive_Endpoint_Returns_OK` ⏳ — awaits ServiceDefaults/MapDefaultEndpoints
- MessagingEndpointTests (8 tests):
  - `Welcome_Returns_OK` ⏳ — GET /api/messaging/welcome
  - `Welcome_Returns_Json_With_Message` ⏳ — JSON shape { message: "..." }
  - `Welcome_Default_Contains_Terrarium` ⏳ — default "Welcome to .NET Terrarium 2.0!"
  - `Motd_Returns_OK` ⏳ — GET /api/messaging/motd
  - `Motd_Returns_Json_With_Message` ⏳ — JSON shape { message: "..." }
  - `Version_Returns_OK` ⏳ — GET /api/messaging/version
  - `Version_Returns_Json_With_Version` ⏳ — JSON shape { version: "..." }
  - `Version_Default_Looks_Like_A_Version` ⏳ — default "1.0.0.0" pattern
- ThrottleTests (5 tests):
  - `Single_Request_Should_Succeed` ✅ — basic happy path
  - `Multiple_Normal_Requests_Should_Succeed` ✅ — 5 sequential requests
  - `Excessive_Requests_Should_Return_429` ⏳ — rate limit enforcement
  - `Throttled_Response_Contains_Retry_After_Header` ⏳ — Retry-After header
  - `Throttled_Response_Body_Contains_Rate_Limit_Message` ⏳ — body text

**Key Decisions:**
- Tests written against expected interfaces from legacy code analysis
- `public partial class Program { }` added to Program.cs for WebApplicationFactory access
- Each ThrottleTests test creates its own WebApplicationFactory to isolate throttle state
- JSON response expectations derived from Gus's MessagingEndpoints.cs pattern (seen on squad/9-server-bootstrap)
- Legacy Throttle.cs (60 req/min, per-IP, Hashtable+Cache) informed the rate-limit test expectations
- Build command: `dotnet test src/Terrarium.Server.Tests/`

### 2025-07-16 — SDK Samples Ported to .NET 10 (#28)

**What was done:**
- Ported legacy `Samples/` creature implementations from .NET Framework 2.0 to .NET 10
- Created `src/Terrarium.Samples/` with 3 standalone sample projects referencing `Terrarium.OrganismBase`
- All 3 samples compile as part of `dotnet build src/Terrarium.sln` — 0 warnings, 0 errors
- PR #122 → `squad/28-sdk-samples` → `squadified`

**Samples Created:**
- **SimpleHerbivore** — Beetle that scans for plants, moves toward them, eats, reproduces. Demonstrates: `Scan()`, `LookFor()`, `BeginMoving()`, `MovementVector`, `BeginEating()`, `WithinEatingRange()`, `BeginReproduction()`, `CanReproduce`, `WriteTrace()`, random wandering. Point allocation: Camouflage 50, Eyesight 50.
- **SimpleCarnivore** — Scorpion that hunts other animals, attacks, kills, eats dead prey. Demonstrates: `BeginAttacking()`, `WithinAttackingRange()`, `IsMySpecies()`, `Species.MaximumSpeed` pursuit. Point allocation: AttackDamage 52, MaximumSpeed 28, Eyesight 20.
- **SimplePlant** — Simplest possible organism. Just attributes + serialization stubs. Plant base class handles reproduction automatically.

### 2025-07-16 — SignalR Integration Tests (#53)

**Test Project Created:** `src/Terrarium.SignalR.Tests/`
- xUnit on net10.0, references `Terrarium.Net` project
- Uses `Microsoft.AspNetCore.TestHost` with self-contained test server (no Terrarium.Server dependency)
- **54 tests total: 48 passing, 6 skipped (rate limiting not yet implemented)**
- Added to `src/Terrarium.sln`

**What's Tested (9 test files):**
- **ConnectionLifecycleTests** (8 tests) — connect, join ecosystem, heartbeat, leave, disconnect, join-leave-rejoin cycle, multiple heartbeats, join multiple ecosystems
- **PeerDiscoveryTests** (7 tests) — peer join broadcasts PeerAnnounce, peer leave broadcasts, AnnouncePeer broadcast, RequestPeerList response, cross-ecosystem isolation, duplicate registration
- **TeleportTests** (7 tests) — targeted teleport delivery, broadcast teleport, assembly payload delivery, idempotency key preservation, teleport to nonexistent peer, source doesn't receive own broadcast
- **PopulationReportingTests** (4 tests) — report broadcasts to others, per-species data, reporter doesn't receive own report, cross-ecosystem isolation
- **ErrorHandlingTests** (7 tests) — hub never throws on any method (JoinEcosystem, LeaveEcosystem, Heartbeat, TeleportCreature, ReportPopulation, RequestWorldState, RequestPeerList)
- **RateLimitingTests** (6 tests, all SKIPPED) — teleport 10/60s, population 2/60s, world state 5/60s, peer list 2/60s, heartbeat 3/60s, RetryAfterMs in error. Waiting for hub rate limiting implementation.
- **ReconnectionTests** (4 tests) — new connection ID on reconnect, must rejoin ecosystem, can request world state after reconnect, disconnect doesn't crash other peers
- **EdgeCaseTests** (10 tests) — heartbeat before/after join/leave, leave without join, valid response structures, rapid join/leave cycles, empty state payload, zero species report, announce without version, concurrent operations no deadlock
- **WorldStateTests** (3 tests) — matching ecosystem ID, timestamp validation, caller-only response (no cross-client leakage)

**Architecture Coverage (per Heisenberg's docs/architecture/signalr-hub-spoke.md):**
- Section 2 (Hub Contract): All 8 ITerrariumHub methods tested
- Section 3 (Client Contract): ReceivePeerAnnounce, ReceiveCreatureTeleport, ReceivePopulationReport, ReceiveError callbacks verified
- Section 5 (Teleportation): Targeted and broadcast teleport, idempotency key, assembly payload
- Section 6 (Peer Discovery): Registration, broadcast, cross-ecosystem isolation
- Section 7 (Connection Lifecycle): Full state machine coverage including reconnection
- Section 8 (Population Reporting): Report broadcast, per-species data, isolation
- Section 9 (Error Handling): Hub-never-throws principle verified for all methods; rate limit tests stubbed with Skip
- Section 9 (Rate Limiting): All 5 method limits documented as skipped tests with exact thresholds from architecture doc

**Key Decisions:**
- Used `Microsoft.AspNetCore.TestHost` instead of `WebApplicationFactory<Program>` because `Terrarium.Server` has pre-existing build errors in `SpeciesEndpoints.cs` (missing `version`/`filter` parameters). This decouples SignalR tests from unrelated server compilation issues.
- `TerrariumHub` lives in `Terrarium.Net` (not `Terrarium.Server`), so the test host can wire it up directly with `MapHub<TerrariumHub>("/hub/terrarium")`.
- Rate limiting tests use `[Fact(Skip = "...")]` — they'll light up when Mike's hub implementation adds rate limiting.
- Factory is `IAsyncLifetime` (xUnit) — server starts/stops per test class via `IClassFixture<TerrariumHubFactory>`.
- Build command: `dotnet test src/Terrarium.SignalR.Tests/`

**Key Porting Changes:**
- Modern C# delegate syntax (`Load += LoadEvent` instead of `new LoadEventHandler(...)`)
- Nullable annotations (`PlantState?`, `AnimalState?`)
- Pattern matching (`organismState is PlantState plant`)
- Updated namespaces to `Terrarium.Samples.*`
- SDK-style `.csproj` with `ProjectReference` to OrganismBase

### 2025-07-16 — Web Renderer Tests (#60)

**Test Project Created:** `src/Terrarium.Web.Tests/`
- xUnit on net10.0, references `Terrarium.Web` and `Terrarium.OrganismBase`
- Compiles `SpriteMetadata.cs` and `TerrariumSprite.cs` from `Terrarium.Game` directly (Game has pre-existing build error in Hosting/CreatureValidator.cs — duplicate `ValidationResult`)
- Copies `animations.json` from `Terrarium.Web/wwwroot/assets/sprites/` as test data
- **142 tests, 0 failures** across 6 test files
- Added to `src/Terrarium.sln`

**What's Tested (6 test files):**
- **SpriteMetadataTests** (24 tests) — Load/deserialize, invalid JSON, FrameSize, CreatureFamilies, GetCreature (case insensitive, unknown), SpriteSheetRef, SheetSize, GetAnimation (known/unknown/case insensitive), AnimationSequence properties, GetFrameRect pixel math (large/small, frame wrapping, single-frame, unknown, defaults)
- **AnimationsJsonIntegrationTests** (18 tests) — Real animations.json validation: deserializes, frame sizes correct, animal sheet refs present, effect creatures may omit small sheet, all animal creatures have 8-direction animations for 5 actions, frame counts positive, frames fit within sheet width, rows within sheet height, frame durations positive, direction-to-row mapping theory tests, sheet dimension theory tests, GetFrameRect with real data
- **TerrariumSpriteTests** (16 tests) — Default values, frame dimensions (48x48), AdvanceFrame from zero (no position move), AdvanceFrame from non-zero (moves position), frame wraps at 10, fractional delta, multi-frame accumulation, full 10-frame cycle, property setters, DisplayAction, selection toggle
- **SpriteDataModelTests** (14 tests) — Record equality/inequality for FrameSizeInfo, SheetDimensions, SpriteSheetRef, SheetSizeInfo, AnimationSequence, CreatureAnimations, SpriteAnimationData; manual frame rect math verification theory tests
- **DirectionMappingTests** (26 tests) — Action+direction→row formula (9 theory cases), 8 directions per action, total 40 rows, no row overlap, DisplayAction→animation key mapping (5 directional + 4 non-directional fallback to idle), frame index wrapping theory tests
- **ViewportMathTests** (19 tests) — World↔screen coordinate transforms: origin, bottom-right, center, scroll offset, outside viewport, zoom; round-trip theory tests; terrain tile index ↔ world position; visible tile range calculation
- **CreatureInfoConventionTests** (5 tests) — Positional record shape validation via reflection, energy percent calculation (normal/zero-max/full/over-max)

**Architecture Coverage:**
- Sprite sheet math: 10 frames/row, 40 rows for animals, 5 actions × 8 directions
- Frame rect calculation: `(startFrame + frameIndex % frameCount) * frameSize` for X, `row * frameSize` for Y
- Teleporter/effect sprites: different layout (16 frames/row, 1 row, no small variant)
- DisplayAction enum ↔ animation action key mapping
- Viewport coordinate transforms: world→screen, screen→world, round-trip invariants
- Terrain grid: tile index calculations, visible tile range

**Key Decisions:**
- Used `Compile Include` for `SpriteMetadata.cs` and `TerrariumSprite.cs` from Terrarium.Game instead of project reference, because Game has pre-existing duplicate `ValidationResult` build error from parallel Heisenberg/Mike work
- Referenced `Terrarium.Web` directly (Web SDK project) to test CreatureInfo record shape
- animations.json copied as `Content` with `CopyToOutputDirectory` for integration tests
- Build command: `dotnet test src/Terrarium.Web.Tests/`
