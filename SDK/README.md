# Terrarium SDK — QA & Infrastructure Assessment

> Assessed by Hank (Tester/QA) — initial exploration pass

---

## Test Infrastructure Overview

### Framework
- **MSTest (Visual Studio Unit Testing Framework)** — `Microsoft.VisualStudio.TestTools.UnitTestFramework` v10.0
- Targets **.NET Framework 4.0**
- Project: `ServerMVC/TerrariumServer.Tests/TerrariumServer.Tests.csproj`
- Project GUID type includes `{3AC096D0-A1C2-E12C-1390-A8335801FDAB}` (VS Test Project)

### Test Files
| File | Tests | What's Covered |
|---|---|---|
| `Controllers/HomeControllerTest.cs` | 2 | `HomeController.Index()` — checks ViewData message; `HomeController.About()` — null check |
| `Controllers/AccountControllerTest.cs` | 13 | `ChangePassword` (GET, POST success, POST fail, POST invalid model); `ChangePasswordSuccess`; `LogOff`; `LogOn` (GET, POST success w/ and w/o return URL, POST invalid model, POST bad credentials); `Register` (GET, POST success, POST duplicate user, POST invalid model) |

### Test Patterns
- **Arrange/Act/Assert** pattern used consistently
- **Hand-rolled mocks**: `MockFormsAuthenticationService`, `MockMembershipService`, `MockHttpContext` — no mocking framework (no Moq, NSubstitute, etc.)
- **Factory method**: `GetAccountController()` centralizes controller construction with injected mock services
- Mock membership service uses hardcoded credential strings (`"goodPassword"`, `"badPassword"`, `"duplicateUser"`) for branching logic

### Coverage Assessment
- **What IS tested:** ASP.NET MVC 2 boilerplate controllers only (Home, Account). These are the auto-generated MVC project template tests — not Terrarium-specific logic.
- **What is NOT tested:** Everything that matters.
  - No tests for any Terrarium game logic
  - No tests for creature behavior (OrganismBase)
  - No tests for P2P networking
  - No tests for server-side creature management, reporting, or ecosystem services
  - No tests for the WPF client
  - No tests for serialization/deserialization
  - No integration tests
  - No tests for the SDK skeletons or samples
- **Verdict: Coverage is effectively zero for domain logic.** The 15 tests that exist are stock MVC template tests. They don't validate anything unique to Terrarium.

---

## SDK Documentation Summary

### What's Available for Creature Developers

#### Documentation (`SDK/Docs/`)
| File | Description |
|---|---|
| `What is Terrarium.doc` | High-level overview of the Terrarium concept |
| `Advanced Developer Guide.doc` | Deep-dive for experienced creature authors |
| `OrganismBase.chm` | Compiled HTML Help for the OrganismBase API |
| `User Interface Guide.doc` | Guide to the Terrarium game client UI |

#### Tutorials (`SDK/Manuals/`)
| File | Description |
|---|---|
| `TUTORIAL_CS.doc` | C# creature development tutorial |
| `TUTORIAL_VB.doc` | VB.NET creature development tutorial |

#### Starter Skeletons (`SDK/Skeletons/`)
Ready-to-use creature templates with full attribute configuration and event handling:
- `CSCarnivore.cs` — C# carnivore: hunt/attack/eat pattern
- `CSHerbivore.cs` — C# herbivore: find plants/eat/hide pattern
- `VBCarnivore.vb` — VB.NET carnivore equivalent
- `VBHerbivore.vb` — VB.NET herbivore equivalent

Key patterns demonstrated in skeletons:
- Assembly-level attributes: `[OrganismClass]`, `[AuthorInformation]`
- Creature attributes: `[CarnivoreAttribute]`, `[MatureSize]`, `[AnimalSkin]`, `[MarkingColor]`
- Point allocation system: 100 points across 7 stats (MaximumEnergy, EatingSpeed, AttackDamage, DefendDamage, MaximumSpeed, Camouflage, Eyesight)
- Event model: `Load`, `Idle`, `Attacked`, `MoveCompleted`
- Base class: `Animal` (from `OrganismBase`)
- Serialization: `SerializeAnimal()` / `DeserializeAnimal()` for teleportation/save

#### Tutorial Solutions (`SDK/Solutions/CS/` and `SDK/Solutions/VB/`)
Progressive exercise solutions (C# and VB.NET):
- **Exercise 1:** Basic herbivore — find plants, eat, reproduce, random movement
- **Exercise 2:** Adds `Attacked` event handler — defend and flee behavior
- **Exercise 3:** Adds `MoveCompleted` event — antenna-based communication with same-species creatures for cooperative pathfinding

Each exercise builds on the previous one, teaching a new OrganismBase API concept.

---

## Samples Inventory

### `Samples/` Directory
Solution file: `Samples.sln` (VS 2005 format)

| Project | File | Description |
|---|---|---|
| `Herbivore/` | `SimpleHerbivore.cs` | Complete herbivore sample — camouflage + eyesight strategy, plant-seeking behavior |
| `Carnivore/` | *(csproj + Properties only, no .cs visible at top level)* | Carnivore sample project |
| `Plant/` | *(csproj + Properties only)* | Plant sample project |

The sample herbivore in `Samples/Herbivore/SimpleHerbivore.cs` is a cleaned-up, XML-documented version of the skeleton — good reference implementation.

---

## Tools Inventory

### `Tools/ServerConfig/`
Server deployment and configuration tool:
- `ServerConfig/` — WinForms app: IIS configuration helper (`IISHelper.cs`), setup wizard (`OldWizardForm.cs`), progress UI, server verification
- `InstallerItems/` — Custom installer components: `WebsiteInstaller.cs`, `CounterCreationDataInfo.cs`, `EventLogInfo.cs`, `PerformanceCounterCategoryInfo.cs`
- `Setup/` — `SqlSetup.sql` (database schema), `web.config` (server config template)

### `Tools/StyleEditor/`
Visual style editing tool for the game UI:
- `Metal/` — Custom "Metal" UI framework: `MetalButton`, `MetalPanel`, `MetalLabel`, `MetalGradient`, `MetalStyleManager`
- `Controls/` — Shared control library
- `StyleEditor/` — Main WinForms application: `MainForm.cs`

---

## Build Status

### Build Command
```
dotnet build Terrraium2010.sln
```
(Note: solution filename has a typo — three R's: `Terrraium2010.sln`)

### Result: **FAILED** — 15 errors, 0 warnings

### Root Causes
1. **Missing .NET Framework 4.0 targeting pack** — `MSB3644` on all 14 ClientWPF projects and the test project. The environment has .NET SDK 10.0.102 but no .NET Framework 4.0 Developer Pack installed.
2. **Missing Visual Studio Web Application targets** — `MSB4278` on `ServerMVC/TerrariumServer/TerrariumServer.csproj`. Requires `Microsoft.WebApplication.targets` from Visual Studio 2010.

### Affected Projects (all of them)
- `ServerMVC/TerrariumServer.Tests` — MSB3644
- `ServerMVC/TerrariumServer` — MSB4278 (Web Application targets)
- All 12 `ClientWPF/*` projects — MSB3644

### Legacy Build Context
- **Main solution:** `Terrraium2010.sln` — VS 2010 format, 15 projects (Server + Client groups)
- **Legacy client solution:** `Client/client.sln` — VS 2008 format, 13 projects (Terrarium, HttpListener, OrganismBase, Configuration, Controls, Game, Services, Glass, Renderer, Terrarium2, Controls2, ControlsWPF, TerrariumWPF)
- **Client version:** `Client/VersionInfo.cs` — Assembly version `2.1.0.*`
- **Samples solution:** `Samples/Samples.sln` — VS 2005 format

### CI/CD Infrastructure
**None exists.** No `.github/workflows/`, no YAML pipelines, no build scripts (`.ps1`, `Makefile`), no CI configuration of any kind.

---

## Recommendations for Test Strategy

### Immediate Priorities

1. **The build must work before tests can run.** The entire solution needs a .NET Framework 4.0 targeting pack or (better) a migration to a modern target framework. Without a green build, we have nothing.

2. **Establish CI/CD immediately.** Zero automation means zero confidence. Even a basic GitHub Actions workflow that attempts to build would catch regressions.

3. **Delete or quarantine the template tests.** The 15 existing MSTest tests are auto-generated MVC boilerplate. They test nothing Terrarium-specific. They give a false sense of coverage. Mark them clearly as template code or remove them.

### Test Strategy for Real Coverage

4. **OrganismBase is the critical test target.** This is the API that creature developers depend on. It needs unit tests for:
   - Point allocation validation (100-point budget across 7 stats)
   - Creature lifecycle events (`Load`, `Idle`, `Attacked`, `MoveCompleted`)
   - Scanning/targeting logic (`Scan()`, `LookFor()`, `WithinEatingRange()`, `WithinAttackingRange()`)
   - Serialization round-trips (`SerializeAnimal` / `DeserializeAnimal`)
   - Species identification (`IsMySpecies()`)
   - Movement and combat state machines

5. **Server API needs integration tests.** The ServerMVC project handles creature registration, ecosystem management, and peer coordination — all untested.

6. **Adopt a mocking framework.** The hand-rolled mocks in `AccountControllerTest.cs` are fine for 3 interfaces but won't scale. Moq or NSubstitute would cut test setup code significantly.

7. **Test the SDK samples build.** The skeletons and samples reference `OrganismBase` — they should compile against it in CI. If a creature developer's first experience is a broken template, we've already lost them.

8. **Framework migration consideration.** If we migrate to xUnit or NUnit during modernization, do it early — before writing hundreds of new tests against MSTest patterns.
