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
📌 Team update (2026-02-11): Solution uses classic .sln (not .slnx), CS1591 suppressed during port — decided by Heisenberg
📌 Team update (2026-02-11): CSS token naming: --glass-{category}-{element}-{modifier}, BEM for components — decided by Jesse

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
