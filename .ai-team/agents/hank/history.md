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
