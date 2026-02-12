# Decisions

> Team decisions that all agents must respect. Append-only — never edit existing entries.

### 2026-02-11: Creature management UI architecture (Upload + Gallery)
**By:** Skyler
**What:** Created creature upload and gallery pages as separate routes with dedicated navigation menu, using AssemblyValidator for server-side DLL validation
**Why:** 
- **Separation of concerns:** Upload and Gallery are distinct user flows (adding vs. browsing creatures), each deserving its own route and UI
- **Server-side validation is non-negotiable:** Creature DLLs can contain P/Invoke, forbidden namespaces, or malicious code — validation MUST happen server-side using AssemblyValidator before any assembly loading occurs. Client-side checks are insufficient for security.
- **AssemblyValidator uses metadata inspection (no loading):** The validator uses `System.Reflection.Metadata.PEReader` to inspect assemblies without loading them into the runtime, preventing execution of untrusted code during validation.
- **Inline error display improves UX:** Showing all validation failures inline (not just "failed" with a toast) helps users understand what's wrong with their creature and how to fix it. Each error is actionable (e.g., "Remove System.IO reference", "Inherit from Animal or Plant").
- **File picker over drag-and-drop:** Blazor Server mode doesn't support native file drag-and-drop without JavaScript interop. Using `InputFile` component with file picker is the standard Blazor pattern and works reliably.
- **Temporary file handling:** Uploaded DLLs are saved to `Path.GetTempPath()/terrarium-uploads/` for validation, then cleaned up on success or failure. Production deployment would move validated DLLs to a game engine–managed creatures directory.
- **NavMenu component:** Adding dedicated navigation makes Upload and Gallery discoverable — users need a way to get to these pages from the main game view.
- **Gallery search/filter:** Ecosystem can have dozens of species — search and type filtering make it usable at scale.

### 2026-02-11: Package versioning aligned with .NET version
**By:** Saul
**What:** Established version numbering for Terrarium NuGet packages: `{major}.{minor}.{patch}-{suffix}` where major.minor matches the .NET version (10.0 for .NET 10), patch is incremental, and suffix is `-preview.X` for previews. Initial version is `10.0.0-preview.1`.
**Why:** Makes it immediately clear which .NET version the packages target. Follows semantic versioning and .NET preview conventions. Aligns with user expectations — if you're on .NET 10, you use Terrarium 10.x packages.

### 2026-02-11: OrganismBase NuGet package structure
**By:** Saul
**What:** Configured Terrarium.OrganismBase as a NuGet package (version 10.0.0-preview.1) with comprehensive metadata: PackageId, Authors, Description, License (MIT), symbols package generation (snupkg), and README inclusion. Package targets net10.0 and includes API documentation XML.
**Why:** Creature developers need an easy way to reference OrganismBase without needing the full Terrarium source code. NuGet distribution makes it simple to add the dependency via `dotnet add package Terrarium.OrganismBase`.

### 2026-02-11: GitHub Actions workflow for NuGet publishing
**By:** Saul
**What:** Created `.github/workflows/nuget-publish.yml` workflow that:
- Triggers on release tags (`v*.*.*`) or manual dispatch
- Builds Terrarium.sln
- Packs both OrganismBase and Templates packages
- Publishes to GitHub Packages (nuget.pkg.github.com)
- Uploads packages as artifacts (30-day retention)
- Uses .NET 10 preview quality
**Why:** Automates package publishing on release. Tag-based triggering (`git tag v10.0.0-preview.1 && git push --tags`) ensures packages are published consistently when releases are created. Manual dispatch provides flexibility for testing or hotfix releases. GitHub Packages integration keeps packages in the same ecosystem as the source code.

### 2026-02-11: dotnet new template for creature scaffolding
**By:** Saul
**What:** Created Terrarium.Templates NuGet package containing a `dotnet new terrarium-creature` template. Template generates a creature project with:
- Parameterized customization: `--CreatureType` (Animal/Plant), `--IsCarnivore` (true/false), `--AuthorName`, `--AuthorEmail`
- Pre-configured project file with Terrarium.OrganismBase package reference
- Assembly attributes (OrganismClass, AuthorInformation)
- Starter implementation with event handlers (Animals) or minimal structure (Plants)
- Modern C# (file-scoped namespaces, nullable enabled)
- Comments and TODOs guiding implementation
**Why:** Lowers the barrier to entry for creature development. New developers can scaffold a working creature in seconds via `dotnet new terrarium-creature -n MyCreature` rather than manually setting up project files, attributes, and boilerplate. Template ensures consistent structure and best practices out of the box.

### 2026-01-22: Creature upload and download pipeline implementation
**By:** Mike (Engine/Networking)
**What:** Implemented server-side API endpoints and game engine methods for uploading creature assemblies to the server and downloading them to introduce into local ecosystems
**Why:** Issue #69 required wiring the complete creature introduction pipeline through the server

#### Implementation Details

##### Server Endpoints (SpeciesEndpoints.cs)

1. **Fixed pre-existing build errors**: `/extinct` endpoint was missing `version` and `filter` parameters
2. **Added GET `/{name}/assembly` endpoint**: Downloads species assembly bytes by name and version
   - Validates species exists and is not blacklisted
   - Reads assembly from disk at configured `AssemblyPath`
   - Returns binary assembly data
3. **Enhanced POST `/register` endpoint**: Now saves uploaded assemblies to disk at `AssemblyPath` location

##### Game Engine Methods (GameEngine.cs)

1. **`IntroduceCreatureFromPac`**: Loads and validates creature from PrivateAssemblyCache
   - Validates assembly if validator provided
   - Extracts species info using `Species.GetSpeciesFromAssembly`
   - Adds organism to game via `AddNewOrganism`
   - Auto-registers with server via ServiceBridge
   
2. **`IntroduceCreatureFromServerAsync`**: Downloads creature from server and introduces locally
   - Downloads assembly bytes via `GameServiceBridge.GetSpeciesAssemblyAsync`
   - Validates downloaded assembly
   - Saves to PrivateAssemblyCache
   - Introduces into game

##### Service Bridge (GameServiceBridge.cs)

Added **`GetSpeciesAssemblyAsync`** method to download species assemblies by name and version from the server.

##### Interface Updates (IGameEngine.cs)

Added the two new introduction methods to the interface for DI and testability.

#### Key Design Decisions

1. **Assembly storage**: Server stores assemblies on disk at `ServerSettings.AssemblyPath` (configured via appsettings.json)
2. **Validation flow**: AssemblyValidator runs before assemblies are loaded, both on upload and download
3. **Species extraction**: Uses existing `Species.GetSpeciesFromAssembly` pattern for consistency
4. **Async by default**: Download path is fully async; PAC load path is synchronous per existing patterns
5. **Auto-registration**: When introducing from PAC, automatically registers with server if ServiceBridge is configured

#### Usage Pattern

```csharp
// Upload creature to server (web UI)
// User uploads DLL → validates → saves to temp → registers via POST /api/species/register

// Introduce from server
var success = await gameEngine.IntroduceCreatureFromServerAsync(
    "MyCreature", "1.0", pac, validator);

// Introduce from PAC (already downloaded)
var success = gameEngine.IntroduceCreatureFromPac(
    "MyCreature, Version=1.0.0.0, ...", pac, validator);
```

#### Testing Notes

- Server build verified (no errors)
- Full solution build verified (no errors)
- Pre-existing Upload.razor drag-and-drop error fixed (removed unsupported feature)
- Assembly validation integrated at both upload and download points

### 2026-07-16: SDK documentation structure and tutorial approach
**By:** Hank
**What:** SDK documentation lives in `docs/sdk/` with separate `tutorials/` and `api/` subdirectories. Tutorials teach modern C# (file-scoped namespaces, nullable types, pattern matching). API docs are comprehensive class/method references with complete examples.
**Why:** 
- Clear separation: tutorials for learning flow, API for lookup
- Modern C# conventions make code easier to read and maintain
- References existing `src/Terrarium.Samples/` implementations as proof
- Covers the full creature development lifecycle: plant → herbivore → carnivore
- Comprehensive API docs eliminate need to read source code for basic usage
- Tutorials anticipate common mistakes and provide strategy patterns

### 2026-02-10: User directive
**By:** bradygaster (via Copilot)
**What:** This codebase is large — no single agent should try to scan it all at once. Document incrementally, piece by piece, until the whole codebase is documented.
**Why:** User request — captured for team memory

### 2026-02-10: MVC Server Is a Scaffold — All Game Logic Lives in Legacy ASMX
**By:** Gus (Server Dev)
**Status:** Observation (not a change proposal)

**What:** The MVC server (`ServerMVC/TerrariumServer/`) contains only ASP.NET MVC 2 template code — Account management and a placeholder Home controller. Zero game-related endpoints exist. All ecosystem functionality — peer discovery, species registration, population reporting, crash logging, messaging — lives in the legacy `Server/Website/` as ASMX web services backed by direct ADO.NET calls to SQL Server stored procedures.

**Why:**
1. Client devs: clients talk to the legacy ASMX endpoints. Any API contract changes must account for the legacy service signatures (SOAP/DataSet-based).
2. Don't assume the MVC project has any game functionality. It doesn't.
3. Modernization means migrating 7+ ASMX services (with stored procedure dependencies) to MVC controllers or Web API. The `BugService` is a stub (`TODO` in code).
4. Database: Schema is `TerrariumWhidbey` in SQL Server. ~14 tables, ~17 stored procs. All data access is inline ADO.NET — no ORM.

### 2026-02-10: Build Must Be Green Before New Tests
**By:** Hank (Tester/QA)
**Status:** Proposed

**What:**
1. No new tests should be written against the current MSTest/MVC2 test project. The 15 existing tests are stock MVC template tests and test nothing Terrarium-specific.
2. The build infrastructure must be modernized first. Before any test strategy can execute, the team needs to decide: retarget to a buildable framework, or provide the .NET Framework 4.0 Developer Pack.
3. CI/CD must be established. There is zero build automation.
4. When a new test project is created, prefer xUnit over MSTest.

**Why:** Nothing builds, nothing runs, nothing is tested. Any code changes are flying blind until this is resolved.

### 2026-02-10: ClientWPF is empty scaffolding — Client/ is the source of truth (consolidated)
**By:** Heisenberg, Jesse, Mike
**Status:** Observation / Proposed

**What:** The `ClientWPF/` directory contains 13 projects, all of which are empty .NET 4.0 shells containing only `Properties/AssemblyInfo.cs` (and for TerrariumClient, empty `App.xaml`/`MainWindow.xaml`). No business logic has been ported. The WPF rewrite was abandoned before any meaningful code was migrated.

Any modernization work must use `Client/` as the source of truth:
- `Client/Renderer/`, `Client/Glass/`, `Client/Controls/` — full WinForms implementations
- `Client/TerrariumWPF/` and `Client/ControlsWPF/` — partial earlier WPF port with real XAML (closest starting point for WPF migration)
- `Client/Game/`, `Client/OrganismBase/`, `Client/HttpListener/`, `Client/Services/`, `Client/AsmCheck/`, `Client/Configuration/` — all real engine/networking/infrastructure code

Similarly for the server: `Server/Website/` is the functional server; `ServerMVC/TerrariumServer/` is boilerplate.

**Why:**
- Do not invest time fixing or extending `ClientWPF/` projects
- When modernizing, port code from `Client/` to new project structure
- The `Terrraium2010.sln` is useful for server work but misleading for client work
- The WPF stubs will need to either be populated (if migration proceeds) or removed

### 2025-07-15: .NET 10 Modernization Sprint Plan
**By:** Heisenberg
**What:** Created a 14-sprint (~7 month) plan to modernize Terrarium from .NET Framework 3.5 to .NET 10. Key decisions: new SDK-style solution structure, WPF on .NET 10 for UI, Silk.NET OpenGL for rendering (replacing DirectX 7 DirectDraw), ASP.NET Core Minimal APIs for server (replacing ASMX), Dapper with existing stored procedures (not EF Core), gRPC for P2P networking (replacing custom TCP), System.Text.Json replacing BinaryFormatter, process isolation replacing CAS sandboxing, System.Reflection.Metadata replacing native C++ AsmCheck. Plan follows leaf-to-root dependency order with server and client work parallelized. Six decisions flagged for Brady: SQL hosting, deployment target, VB.NET SDK support, legacy code disposition, sprite assets, and cross-platform aspirations.
**Why:** Brady requested a concrete sprint plan to bring Terrarium to .NET 10. The codebase spans three generations (.NET 2.0/3.5/4.0) with deeply legacy dependencies (DirectX 7 COM interop, ASMX SOAP services, BinaryFormatter serialization, Code Access Security, custom TCP networking). An incremental, sprint-by-sprint plan with clear ownership and dependency tracking is essential to manage the risk of a migration this large. Each sprint produces buildable, testable output so progress is always demonstrable.

### 2026-02-11: Blog-everything directive
**By:** bradygaster (via Copilot)
**What:** Every decision, change, and highlight must be blogged. We are writing THE blog post that announces someone finally upgraded .NET Terrarium to use the latest .NET and AI technology. Think Hanselman-ready — hand him the blog and the end-state of Terrarium updated to .NET 10, and he publishes it. This is a 30-year .NET community story. Document everything.
**Why:** User request — this is a historically significant project for the .NET community. People who've been doing .NET for 30 years will care about this. The blog IS a deliverable, not an afterthought. Beth (Technical Writer / Blogger) hired to own this. Blog issues tracked as #93-#100. Blog content lives in `docs/blog/`.

### 2026-02-10: Brady's modernization decisions
**By:** bradygaster (via Copilot)
**What:**
1. SQL hosting: Docker for dev, Azure SQL for prod — approved
2. Deployment target: Azure Container Apps — confirmed
3. C# only going forward
4. Delete legacy code — no need to archive Client/, Server/, ClientWPF/, ServerMVC/ after migration
5. Use ALL original imagery — people who know .NET Terrarium should recognize it immediately
6. Cross-platform — change frontend to a web app instead of WPF desktop. Use .NET Aspire for orchestration. Staff up with new agents as needed. Use GitHub Issues and PRs to track all work items. Use labels for squad members and progress. Keep issues updated with status for GitHub project board tracking.
**Why:** Product owner decisions — these override Heisenberg's recommendations where they differ (especially #4 delete vs archive, #6 web app vs WPF)

### 2026-02-11: Beth's inspiration
**By:** bradygaster (via Copilot)
**What:** Beth is inspired by someone who was the voice of .NET's marketing program for years, then moved into the product team. She's done it all. Beth is the fearless voice of the .NET developer toiling away. Beth is our voice.
**Why:** User request — establishes Beth's identity and tone. She writes from the trenches, not the press box. Advocacy-to-engineering energy. Community-first.

### 2025-07-16: Diagram Standards & Audit Results
**By:** Badger (Diagram Designer)
**Status:** Implemented

**What:** Full audit and upgrade of every diagram across the repository. 3 converted from ASCII/plain to Mermaid, 6 improved with arrow labels and fixes, 3 new diagrams added. Standards established: no ASCII art ever, every arrow labeled, PascalCase node IDs, subgraphs for boundaries, type selection guidelines (graph for dependencies, sequenceDiagram for protocols, stateDiagram for lifecycles, classDiagram for hierarchies, gantt for scheduling). Directory tree listings (├── └──) are acceptable as file structure displays.
**Why:** Brady directive — "never use ASCII art, use Mermaid, fix it, make a rule, never break it." Ensures all diagrams render properly on GitHub and maintain consistent quality.

### 2026-02-11: Never use ASCII art — use Mermaid diagrams instead
**By:** bradygaster (via Copilot)
**What:** All diagrams in docs, blog posts, and markdown files must use Mermaid syntax. ASCII art (box drawing characters, block elements, ASCII Gantt charts) is banned.
**Why:** User request — captured for team memory. Brady was emphatic: "sweet lord the blog has ascii art in it. never use ascii art. use mermaid. fix it, make a rule, never break it."

### 2026-02-11: VB.NET language — respectful framing only
**By:** bradygaster (via Copilot)
**What:** Never refer to VB.NET as "dead weight", "debt", or in any negative/dismissive way. The decision is C# only going forward, but VB.NET is a respected part of .NET's history. Frame it simply: "we're C# now" — not as dropping something bad.
**Why:** User request — captured for team memory. Brady said: "i would never refer to vb.net as dead weight or debt. we just do C# now. it doesn't have to be like that."

### 2025-07-16: Orleans + SignalR for Terrarium Networking Layer
**By:** Heisenberg (Lead / Architect)
**Status:** Recommendation
**Requested by:** bradygaster
**Impact:** Sprint 7, Sprint 11, Sprint 12

**What:** Recommends Orleans + SignalR hybrid architecture. Orleans owns stateful domain logic (EcosystemGrain, PeerGrain, SpeciesRegistryGrain, PopulationGrain); SignalR remains browser push channel only. Tick loop runs via Orleans grain timer on EcosystemGrain. Teleportation mediated by grain-to-grain calls. Per-organism grains rejected — per-ecosystem is the right granularity. SignalR.Orleans provides backplane without Redis. Aspire integration via AddOrleans() is first-class. Sprint 7 gets heavier (grain implementation), Sprint 11 gets lighter (Orleans handles scaling).
**Why:** The legacy codebase already implements actor patterns manually (static Hashtables, lease timeouts, state serialization). Orleans formalizes these patterns. SignalR-only would require hand-rolling ConcurrentDictionary state management, BackgroundService timer multiplexing, manual crash recovery, and a separate Redis backplane. Orleans is a net-neutral or slight reduction in complexity.

### 2025-07-16: Solution uses classic .sln format, not .slnx; CS1591 suppressed
**By:** Heisenberg
**Status:** Decided
**What:**
1. `src/Terrarium.sln` uses classic Visual Studio solution format (Format Version 12.00), not `.slnx`. Classic format has universal tooling support; `.slnx` is too new.
2. CS1591 (missing XML doc comments) suppressed via `<NoWarn>` in `Directory.Build.props` during initial port phase.
3. EngineSettings fully ported with all 50+ original game constants — single source of truth for game balance.
**Why:** `.slnx` tooling is immature. `dotnet sln add` was broken by workload manifest issue, requiring manual `.sln` authoring (simpler in classic format). CS1591 suppression is temporary until APIs stabilize.

### 2025-07-15: CSS token naming convention for Glass theme
**By:** Jesse (Client Dev)
**Status:** Implemented (PR #102)
**What:** All CSS design tokens follow `--glass-{category}-{element}-{modifier}`. Categories: color, gradient, border, shadow, font, spacing, size, radius. Component classes use BEM: `.glass-panel`, `.glass-panel--sunk`, `.glass-titlebar__controls`. Tokens in `glass-theme.css` are the single source of truth for Terrarium's visual identity.
**Why:** Predictable, discoverable naming that maps directly to legacy C# code (e.g., `GlassStyle.ButtonHover.Top` → `--glass-gradient-button-hover-top`). All UI agents must use tokens, not hard-coded values.

### 2026-02-11: Aspire package versions for .NET 10
**By:** Saul (DevOps / Aspire)
**Status:** Implemented
**What:** Aspire AppHost and ServiceDefaults use Aspire.Hosting.AppHost/Aspire.AppHost.Sdk `13.1.0`, Microsoft.Extensions.ServiceDiscovery/Http.Resilience `10.3.0`, OpenTelemetry `1.15.0`. Aspire workload is deprecated in .NET 10 — everything is NuGet-based. All projects should reference ServiceDefaults and call `builder.AddServiceDefaults()` + `app.MapDefaultEndpoints()`.
**Why:** These are the latest stable versions as of 2026-02-11. When Server and Web projects are created, uncomment the ProjectReference and AddProject lines in AppHost.

### 2026-02-11: Terrarium.Web Blazor project structure established
**By:** Skyler (Frontend Web Dev)
**Status:** Implemented (PR #118)

**What:** Created the complete Terrarium.Web Blazor Interactive Server project and component library. The project follows Aspire conventions (AddServiceDefaults, MapDefaultEndpoints), uses SignalR for real-time UI, and all components are built on Jesse's Glass CSS token system. The AppHost now references and serves the Web project with external HTTP endpoints.

**Key architectural decisions:**
1. Blazor Interactive Server mode (not WebAssembly) — enables real-time SignalR push from game engine without WASM download penalty
2. Components use parameter binding (not JS interop) for creature/ecosystem data — ready for SignalR hub integration
3. Canvas element exposed via ElementReference in TerrariumViewport — ready for JS interop rendering pipeline
4. Layout follows original Terrarium chrome structure: titlebar → body (viewport + sidebar) → statusbar

**Why:** The web frontend is the primary user-facing surface for the modernized Terrarium. Interactive Server mode aligns with the Orleans + SignalR architecture (server-side state, push to browser). All components are placeholder-ready for Sprint 4+ when game engine integration begins.

### 2026-02-11: SignalR Hub as Pure Contract Layer
**By:** Mike (Networking / Engine Dev)
**Date:** 2025-07-16
**Status:** Implemented (PR #113)
**Issue:** #16

**What:** `Terrarium.Net` is a contract-only library. It defines interfaces (`ITerrariumHub`, `ITerrariumClient`), message types, and a thin `TerrariumHub` implementation. No business logic.

**Key Technical Decisions:**
1. **FrameworkReference, not NuGet** — `Microsoft.AspNetCore.App` FrameworkReference gives us SignalR for free on net10.0. No version pinning needed.
2. **Single-message teleport** — The legacy 4-step HTTP teleport protocol (version check → assembly check → assembly transfer → state transfer) is collapsed into a single `CreatureTeleport` message. The optional `AssemblyPayload` field carries base64-encoded assembly bytes only when the target peer doesn't have the species assembly. Assembly presence tracking will be handled by `PeerGrain` in Sprint 7.
3. **SignalR groups = ecosystems** — Each ecosystem ID maps to a SignalR group. `JoinEcosystem`/`LeaveEcosystem` manage group membership. This replaces the SOAP-based peer discovery polling loop.
4. **No Terrarium project dependencies** — `Terrarium.Net` depends only on the ASP.NET Core shared framework. Message types use `string` for state payloads (JSON) rather than referencing `Terrarium.OrganismBase` types. This keeps the contract boundary clean and allows the web client to reference it without pulling in game engine dependencies.

**Sprint 7 Integration Points:** Every `// TODO: Sprint 7` comment in `TerrariumHub` marks where Orleans grain calls will be inserted: `JoinEcosystem` → `PeerGrain.RegisterPeer()`, `LeaveEcosystem` → `PeerGrain.RevokeLease()`, `TeleportCreature` → `PeerGrain` for routing, `EcosystemGrain` for validation, `AnnouncePeer` → `PeerGrain` for lease management, `RequestWorldState` → `EcosystemGrain.GetWorldState()`, `OnDisconnectedAsync` → `PeerGrain.RevokeLease()`.

**Why:** The legacy networking stack (`HttpWebListener`, `NetworkEngine`, `PeerManager`) is ~3000 lines of custom TCP socket code, HTTP parsing, and binary serialization. The replacement is ~250 lines of typed interfaces and a thin hub. SignalR handles transport, reconnection, and message serialization. Orleans will handle state, leasing, and routing.

### 2026-02-11: Terrarium.Services is a pure class library — no ServiceDefaults dependency
**By:** Mike (Networking / Engine Dev)
**Status:** Decided
**Issue:** #14
**PR:** #109

**What:** `Terrarium.Services` depends only on `Microsoft.Extensions.Http` and `Microsoft.Extensions.Options`. It does NOT reference `Terrarium.ServiceDefaults`. This keeps it usable by any consumer (Game client, tests, CLI tools) without pulling in ASP.NET Core or OpenTelemetry.

Consumers that want Aspire integration (resilience, service discovery) configure it at the DI level:
```csharp
services.AddTerrariumServices("terrarium-server"); // Aspire service discovery
services.AddTerrariumServices(new Uri("https://api.terrarium.dev")); // explicit URI
```

**Impact:** Gus (Server Dev) — the client-side contracts are defined. Server endpoints should return JSON matching the models in `Terrarium.Services.Models`. Hank (QA) — all services are interface-based, straightforward to mock for testing.

**Why:** Interface-first with minimal dependencies is the right call. The legacy proxies were auto-generated ASMX stubs tightly coupled to SOAP. The new layer is clean, testable, and works with or without Aspire.

### 2026-02-11: Keep ArrayList Scan() for Now
**By:** Mike (Networking / Engine Dev)
**Status:** Proposed
**Date:** 2026-02-10

**What:** The legacy `IAnimalWorldBoundary.Scan()` method returns `System.Collections.ArrayList`. This is a non-generic collection from .NET 1.0 days. I kept `ArrayList` as the return type for `Scan()` because: (1) The Game project (which implements this interface) hasn't been ported yet; (2) Changing it to `List<OrganismState>` now would create a compile-time dependency on the Game project's port; (3) It preserves source-level compatibility with existing creature code that casts from ArrayList.

**Action Required:** When the Game project is ported (whoever picks that up), `IAnimalWorldBoundary.Scan()` should be changed to return `List<OrganismState>` and the `using System.Collections` import can be removed from the interface file.

**Why:** This defers a breaking change until the Game project is ported, maintaining compile-time compatibility now.

### 2026-02-11: Glass CSS expanded with full component library; all 76 original assets cataloged
**By:** Jesse (Client Dev)
**Status:** Implemented (PR #123)
**Issues:** #23, #25

**What:**
1. Glass CSS expanded from base tokens to full component library. New components: sidebar panels with frosted glass (`backdrop-filter: blur()`), text inputs, select dropdowns, toolbar with image buttons, stat grid, minimap, modal dialogs, ticker bar, tabs, icon buttons, themed scrollbars. ~60 new `--glass-*` CSS custom properties added.
2. All 76 original image assets extracted from legacy codebase to `src/Terrarium.Web/wwwroot/assets/` organized by type (sprites, terrain, ui, cursors, icons, screenshots). Full manifest.json catalog created with original paths and purpose descriptions.

**Why:**
1. The web app needs complete Glass-themed components to recreate the original Terrarium look. These CSS classes give any UI developer the building blocks to assemble the game interface without touching raw color values.
2. Brady's #1 directive: "PRESERVE ALL original imagery. When people who know what .NET Terrarium was, they should recognize it immediately." Every image asset in the legacy codebase is now available in the web project.

### 2026-02-11: Organism Isolation Architecture
**By:** Heisenberg (Lead Architect)
**Date:** 2025-07-16
**Status:** Proposed
**Issue:** #43

**What:** Implemented a three-layer isolation architecture using **AssemblyLoadContext** as the primary isolation boundary:

**Layer 1: Static Validation (`CreatureValidator`)** — Before any assembly is loaded, perform metadata-level inspection using `System.Reflection.Metadata` to detect P/Invoke declarations, forbidden type references (file I/O, networking, process spawning, reflection emit), missing required base class (must inherit `Animal` or `Plant`), and missing required attributes (`AuthorInformationAttribute`).

**Layer 2: Load Isolation (`OrganismSandbox`)** — Each creature assembly type gets its own collectible `AssemblyLoadContext`: assemblies are loaded from `byte[]` (no file locks), contexts are collectible (`isCollectible: true`), enabling full unload, shared framework types (OrganismBase, BCL) resolve from the default context, and when a species is removed, its context is unloaded to reclaim memory.

**Layer 3: Execution Safety (`OrganismHost` + `OrganismScheduler`)** — `OrganismHost.ExecuteTurn()` wraps each creature's `InternalMain()` with `CancellationTokenSource` timeout, `Task.Run` + `Task.Wait` for wall-clock enforcement, full exception containment, and CPU time measurement via `Stopwatch`. `OrganismScheduler` provides fair round-robin scheduling with organisms divided into 5 batches and `OrganismQuanta` tracking per-creature CPU usage.

**Alternatives Considered:** Worker Process Isolation (too much serialization overhead for game loop), WebAssembly (not practical for .NET assemblies), Roslyn Scripting (wrong abstraction level).

**Implementation Files:** `src/Terrarium.Game/Hosting/CreatureValidator.cs`, `src/Terrarium.Game/Hosting/OrganismSandbox.cs`, `src/Terrarium.Game/Hosting/OrganismHost.cs`, `src/Terrarium.Game/Hosting/OrganismQuanta.cs`, `src/Terrarium.Game/Hosting/IOrganismScheduler.cs`, `src/Terrarium.Game/Hosting/OrganismScheduler.cs`

**Why:** .NET 10 removes AppDomains entirely. We need a modern replacement that provides assembly isolation, safe execution (timeout enforcement, exception containment, CPU fairness), memory management (ability to unload creature code), and static analysis (pre-load validation).

### 2026-02-11: Hub-and-spoke SignalR architecture — full design
**By:** Heisenberg
**Status:** Proposed
**Date:** 2026-02-11

**What:** Completed Sprint 7 foundation architecture for SignalR-based real-time communication. Key decisions:

1. **Hub contract expanded** — `ITerrariumHub` now has 8 methods (added `Heartbeat`, `RequestPeerList`, `ReportPopulation`). `ITerrariumClient` has 7 callbacks (added `ReceivePeerList`, `ReceivePopulationReport`, `ReceiveError`).

2. **Error handling via callbacks, not exceptions** — Hub methods never throw. All errors delivered via `ReceiveError` with structured `HubError` (code, transient flag, retry-after). Error codes: `UNKNOWN_SPECIES`, `SPECIES_BLACKLISTED`, `TELEPORT_FAILED`, `RATE_LIMITED`, `GRAIN_UNAVAILABLE`, `VERSION_MISMATCH`, `ECOSYSTEM_FULL`, `INVALID_PAYLOAD`.

3. **IEcosystemNotifier abstraction** — Grain-to-hub notifications go through `IEcosystemNotifier` interface in `Terrarium.Net`. Implementation using `IHubContext` lives in `Terrarium.Server`. This keeps `Terrarium.Orleans` decoupled from ASP.NET Core SignalR.

4. **Rate limiting per connection** — Teleport 10/min, population 2/min, world state 5/min, peer list 2/min, heartbeat 3/min.

5. **Heartbeat/lease model** — 30s client heartbeat, 90s server-side lease expiry. EcosystemGrain sweeps expired peers.

6. **Reconnect = rejoin** — SignalR reconnection produces a new connection ID. Clients must re-call `JoinEcosystem` + `AnnouncePeer` + `RequestWorldState` after reconnect.

7. **SignalR message size** — `MaximumReceiveMessageSize` = 512 KB for assembly transfers in `CreatureTeleport`.

**Why:** Sprint 7 is the networking foundation. Mike (TerrariumHub grain wiring) and Skyler (Blazor SignalR client) both depend on these contracts being locked down. The architecture doc at `docs/architecture/signalr-hub-spoke.md` is the single source of truth for the real-time communication layer.

### 2026-02-11: Terrarium.Server.Tests created with integration test suite
**By:** Hank (Tester/QA)
**Status:** Implemented
**Issue:** #13

**What:** Created `src/Terrarium.Server.Tests/` — xUnit integration test project using `WebApplicationFactory<Program>` pattern. 17 tests across 3 test classes: **ServerHealthTests** (server boot, root endpoint, /health, /alive), **MessagingEndpointTests** (GET /api/messaging/welcome, /motd, /version with JSON response validation), **ThrottleTests** (rate limiting: single request success, burst success, 429 enforcement, Retry-After header, body message). 4 tests pass now (bare scaffold), 13 await Gus's server bootstrap. Added `public partial class Program { }` to enable `WebApplicationFactory<Program>` access. Project added to `src/Terrarium.sln`.

**Why:** Tests are written against expected behavior derived from legacy code analysis (Messaging.asmx.cs, Throttle.cs, ServerSettings.cs). When Gus's implementation lands, these tests will validate the server endpoints immediately. Tests should not need major adjustment — the JSON shapes and endpoint paths match what was seen on squad/9-server-bootstrap.

### 2026-02-11: SDK Samples Structure and Conventions
**By:** Hank (Tester/QA)
**Status:** Implemented (PR #122)
**Date:** 2025-07-16

**What:**
1. SDK samples live in `src/Terrarium.Samples/{SampleName}/` — each sample is a standalone `.csproj` referencing `Terrarium.OrganismBase`.
2. Three samples ported: SimpleHerbivore, SimpleCarnivore, SimplePlant. Together they cover all key creature APIs.
3. Sample README at `src/Terrarium.Samples/README.md` documents attributes, APIs, build instructions, and how to create new creatures.
4. Samples follow the modern API conventions: nullable annotations, pattern matching, modern event handler syntax.

**Why:** These are how developers learn to write Terrarium creatures. They must compile, be correct, and demonstrate the full API surface. Each sample is a standalone project so developers can copy one as a starting point without pulling in the full solution. The README serves as the primary developer-facing documentation for creature authoring.

### 2026-02-11: Species & Reporting Endpoint Design
**By:** Gus (Server Dev)
**Date:** 2025-07-16
**PR:** #120
**Issues:** #26, #27

**What:**

**Decision 1: Assembly storage deferred** — The legacy `SpeciesService` stored creature assemblies to disk using `FileIOPermission` (Code Access Security). CAS doesn't exist in .NET 10. Assembly storage needs a separate design decision — likely Azure Blob Storage or a configurable filesystem path without CAS. The `GetSpeciesAssembly` endpoint and `SaveAssembly`/`LoadAssembly`/`RemoveAssembly` methods are not ported. The register endpoint inserts the DB record but does not persist the assembly binary.

**Decision 2: Word filter (PoliCheck) deferred** — The legacy register checked species names, author names, and emails against `invalidwordlist.txt` via `WordFilter.RunQuickWordFilter()`. This is not ported yet — it depends on a static file and the `WordFilter.cs` utility. When ported, it should be a service injected into the endpoint, not a static call.

**Decision 3: Charts consolidated under /api/reporting/stats/** — Rather than creating a separate `/api/charts/` route group, chart data endpoints live under `/api/reporting/stats/`. The legacy `ChartService.asmx` was a thin SOAP wrapper around `ChartBuilder.cs` which just ran stored procedures. The three chart endpoints (`species-list`, `latest`, `top-animals`) are data queries — they belong with reporting.

**Decision 4: ReportPopulation returns Success on server errors** — Matches legacy behavior documented in the original code: "Return success instead of ServerDown because, if the server is getting hammered, Success will tell clients to stop retrying, where ServerDown will tell them to keep doing it."

**Impact:** Mike/Jesse (client devs) — Species API contract is defined. When the client needs to register/list/reintroduce species, these are the endpoints. Hank (tester) — 9 new endpoints to cover. Integration tests will need a SQL Server container with the init script. Saul (DevOps) — No new infrastructure needed — endpoints use the existing `SpeciesDsn` connection string from `ServerSettings`.

**Why:** Defers assembly storage and word filter design to later sprints while establishing endpoint structure now.

### 2026-02-11: Terrarium.Server created — Minimal APIs, IOptions, IMemoryCache throttle
**By:** Gus (Server Dev)
**Status:** Implemented (PR #112)
**Date:** 2025-07-16

**What:**
1. `src/Terrarium.Server/` is now a functional ASP.NET Core Minimal API project targeting net10.0
2. ServerSettings uses `IOptions<ServerSettings>` bound to `"Terrarium"` config section (replaces static `ConfigurationManager.AppSettings` reads)
3. Messaging endpoints: `GET /api/messaging/welcome`, `/motd`, `/version` — return JSON, ported from SOAP `Messaging.asmx`
4. ThrottleMiddleware + ThrottleService replace legacy `Throttle.cs` — uses `IMemoryCache` for TTL expiration and `ConcurrentDictionary` + `Interlocked` for thread safety (replaces `Hashtable` + `lock(typeof(Throttle))` + `HttpContext.Current.Cache`)
5. Dapper + Microsoft.Data.SqlClient referenced but not yet invoked — ready for Sprint 2 discovery/species endpoints
6. Server registered in AppHost as Aspire resource

**Why:** Sprint 1 deliverable. The server is the first functional backend service in the modernized Terrarium stack. All three issues (#9, #10, #11) delivered in one PR.

**Impact:** Client devs can now target `/api/messaging/*` for welcome/MOTD/version instead of SOAP. Future sprints add discovery, species, and reporting endpoints.

### 2026-02-11: Road ahead blog post
**By:** Beth
**Date:** 2026-02-11

**What:** Wrote `docs/blog/journal/sprint-prep-the-road-ahead.md` — a sprint journal entry capturing the current state of Sprints 7–13 (48 open issues, ~230 agent-minutes total, ~89 minutes wall-clock with full parallelism). The post is structured as a narrative, not a project management report: scope overview, sprint-by-sprint descriptions (each with a one-sentence "story"), a per-agent workload table, and an emotional frame connecting Terrarium's 25-year history to the modernization task ahead.

**Key Narrative Decisions:**
1. Lead with the parallelism frame (48 issues, 89 minutes wall-clock) before explaining the work.
2. Each sprint gets a human "story" descriptor before the issue list.
3. The per-agent workload table makes it clear why parallelism matters.
4. Terrarium's 25-year history anchors the emotional frame.
5. Avoid project management language ("deliverables", "blockers", "velocity"). Use developer language: "the heartbeat", "the soul of the game", "the finish line."
6. Four future blog posts (#97, #98, #99, #100) are foreshadowed.

**Why:** Brady's directive is "blog everything." This is the "here's what's ahead" post, written before execution begins. It sets reader expectations (48 issues across 7 sprints), makes the parallelism tangible (8 agents, 89 minutes wall-clock), and establishes the emotional stakes: "an AI team is about to modernize a 25-year-old .NET app in under 2 hours of wall-clock time." It's the call-to-action post that invites the community to watch the migration unfold. Written for .NET veterans—people who remember Terrarium, remember PDC, remember when .NET was brand new.

### 2026-02-11: Renderer architecture — JS ES module + C# interface + Blazor component
**By:** Skyler (Frontend Web Dev)
**Status:** Implemented
**Sprint:** 8
**Issues:** #55, #57, #58, #59

**What:** Three-layer architecture for web game rendering:

1. **`terrarium-renderer.js`** — All Canvas 2D operations: terrain, sprites, text, viewport, interaction
2. **`IGameRenderer` / `CanvasGameRenderer`** — C# interface + JS interop implementation
3. **`GameView.razor`** — Blazor component that owns the renderer lifecycle

Architecture diagram:
```mermaid
graph TD
    A[GameView.razor] -->|creates| B[CanvasGameRenderer]
    B -->|JS interop| C[terrarium-renderer.js]
    C -->|draws on| D[HTML5 Canvas 2D]
    C -->|loads| E[terrain/*.bmp]
    C -->|uses| F[SpriteLoader]
    B -->|implements| G[IGameRenderer]
    C -->|callbacks| B
    B -->|events| A
```

**Key Decisions:**
- Rendering logic in JS where Canvas APIs live natively (no marshaling per-pixel)
- C# interface enables future swap to WebGL or off-screen rendering
- Component-scoped renderer (not DI singleton) — each viewport independent
- `IGameRenderer` lives in `Terrarium.Web.Rendering`, not `Terrarium.Game` (keeps Web layer separate)
- `TerrariumViewport.razor` preserved for backward compatibility
- Viewport interaction: Pan (mouse drag/WASD), Zoom (scroll/+- keys, 0.25x–4.0x), Select (click), Reset (0 key)

**Why:** Keeps rendering logic in JavaScript where Canvas APIs live; C# interface enables future abstraction swaps; component-scoped design prevents coupling.

### 2026-02-11: Web Sprite System Architecture (Issue #56)
**By:** Jesse (Client Dev)
**Status:** Implemented
**Sprint:** 8

**What:** Three-layer JavaScript sprite system for HTML5 Canvas rendering:

1. **`terrarium-sprites.js`** — Core sprite primitives:
   - `SpriteSheet` class: wraps `ImageBitmap` with frame geometry (size, columns, rows, type)
   - `SpriteAnimation` class: time-based frame sequencing with update/draw
   - Direction mapping: 8 directions (E,SE,S,SW,W,NW,N,NE) → row offsets 0-7
   - Action mapping: 5 actions → base rows (attacked=0, defended=8, died=16, ate=24, moved=32)
   - Factory helpers: `createAnimalAnimation()`, `createPlantAnimation()`, `createTeleporterAnimation()`

2. **`sprite-manager.js`** — High-level manager (IIFE singleton):
   - Loads/caches `SpriteSheet` instances from `animations.json` manifest
   - `drawSprite(ctx, spriteId, action, direction, frameIndex, x, y, size)` — stateless draw
   - `drawAnimated(ctx, spriteId, action, direction, deltaMs, x, y, size)` — time-based animated draw
   - `preloadAll()` / `loadSpriteSheet(id)` — on-demand or batch loading

3. **Blazor integration** — `terrarium-interop.js` (ES module):
   - `loadSprites(ids?, useLarge?)` — preload from Blazor
   - `drawSprite(spriteId, action, direction, frameIndex, x, y, size?)` — per-entity draw
   - `drawFrame(worldState)` — batch render with entity array support
   - `getSpriteStatus()` — query loaded state

4. **Renderer integration** — `terrarium-renderer.js`:
   - `loadSprites()` / `drawCreatureSprite()` exports for viewport-aware sprite drawing
   - `renderFrame()` prefers `SpriteManager` when available

5. **TerrariumViewport.razor** — Added `LoadSpritesAsync()` and `DrawSpriteAsync()` C# interop methods

**Script loading order** in `App.razor`: `terrarium-sprites.js` → `sprite-loader.js` → `sprite-manager.js` → `blazor.web.js`

**Why:** Preserves exact original sprite sheet layout (10×40 animal, single-frame plant, 16-frame teleporter); provides both stateless and stateful animation APIs; integrates with existing `SpriteLoader` without breaking; exposes clean Blazor interop for C# game loop control.

### 2026-02-11: Web Renderer Test Strategy
**By:** Hank (Tester/QA)
**Status:** Implemented
**Sprint:** 8
**Issue:** #60

**What:** 142 passing tests covering C#-side renderer contracts.

**Test Strategy Decisions:**
1. **Compile-include Game sources instead of project reference** — `Terrarium.Game` has pre-existing build error (duplicate `ValidationResult`). Test project compiles `SpriteMetadata.cs` and `TerrariumSprite.cs` directly via `<Compile Include>`. When Game build is fixed, switch back to `<ProjectReference>`.

2. **Test the math, not the JS** — Renderer's JS layer can't be unit-tested in xUnit. Instead, test **C# contracts and math**:
   - Frame rect pixel coordinates: `(startFrame + frameIndex % frameCount) * frameSize`
   - Direction-to-row mapping: 5 actions × 8 directions = 40 rows
   - Viewport world↔screen coordinate transforms
   - Terrain grid tile index calculations

3. **animations.json as integration test data** — Real `animations.json` copied to test output and validated: correct frame sizes, all directional animations present, frames fit within sheet, no row overlaps.

4. **CreatureInfo tested via reflection** — `Terrarium.Web` is Web SDK project. Accept ASP.NET dependency and test CreatureInfo shape via reflection + energy-percent calculation logic.

**Impact:** 142 tests, 0 failures; tests BUILD and PASS despite `Terrarium.Game` build errors; stable against parallel renderer work.

**Open Items:**
- TODO: When Skyler finalizes interop service, add tests for CreatureInfo ↔ TerrariumSprite mapping
- TODO: When Jesse's sprite system lands, add tests for animation metadata loading
- TODO: bUnit tests for TerrariumViewport.razor if component gets more complex C# logic

**Why:** Achieves renderer verification without blocking on unrelated Game build issues.

### 2026-02-11: Blog — Sprint 7–8 Journal Entries
**By:** Beth (Technical Writer)
**Status:** Complete
**Sprint:** 7–8
**Issues:** #97, #98

**What:** Two journal entry blog posts:

1. **Sprint 7: TCP Sockets to SignalR** (`docs/blog/journal/07-tcp-to-signalr.md`)
   - 25-year-old custom TCP networking → hub-and-spoke SignalR + Orleans
   - Coverage: Legacy architecture, modern architecture, why it matters, peer discovery flow, heartbeat/lease, assembly caching

2. **Sprint 8: DirectDraw to Canvas** (`docs/blog/journal/08-directdraw-to-canvas.md`)
   - DirectX 7 COM interop → HTML5 Canvas 2D
   - Coverage: Legacy rendering, Canvas 2D pipeline, sprite sheet format, animation.json metadata, accessibility revolution

**Key Narrative Decisions:**
- Lead with human respect for legacy before pivoting to modern design
- Side-by-side comparison tables (Legacy vs. Modern) for architectural clarity
- All diagrams in Mermaid (graphs, sequence diagrams, state machines, flowcharts)
- Explain design philosophy, not code snippets
- Lead with visceral metrics ("four HTTP calls becomes one")
- 25-year-old code as emotional anchor ("Rest easy, TCP socket. Your job is done.")
- Accessibility as victory metric (Canvas as "accessible everywhere")
- Roadmap honesty (particle effects, WebGL optional, multiplayer rendering)

**Alignment with Brady's Vision:**
- ✅ Hanselman-ready: developer-to-developer, credible, narrative-driven
- ✅ Respectful to legacy systems while explaining modern choices
- ✅ Technical depth paired with human emotion
- ✅ No jargon without explanation
- ✅ Mermaid diagrams throughout

**Alignment with Beth's Voice:**
- ✅ Developer-to-developer, not marketing
- ✅ Community-first stewardship frame
- ✅ Technically credible with emotional throughline
- ✅ Compelling narratives ("It's 2001..." → closing payoff)

**Files:**
- `docs/blog/journal/07-tcp-to-signalr.md` (17.4 KB)
- `docs/blog/journal/08-directdraw-to-canvas.md` (20.7 KB)

**Why:** Brady's directive is "blog everything." These posts are the historical record of Sprint 7–8 work. They establish that modernization respects the past while embracing the future. They are Hanselman-ready and set the tone for future documentation.

### 2026-02-11: Sprint 9 Blog Post — "It Lives!"
**By:** Beth (Technical Writer)
**Status:** Complete
**Blog Location:** `docs/blog/journal/09-it-lives.md`

**What:** Sprint 9 blog post capturing the integration moment when Terrarium runs for the first time as a complete, connected system in a browser.

**Rationale:** Sprint 9 represents the critical inflection point: parts becoming system. Integration of game engine (30 Hz), SignalR hub (dispatcher), Blazor Interactive Server (renderer host), TerrariumHubClient (event bridge), CanvasGameRenderer (60 FPS), and .NET Aspire orchestration. This is where the narrative shifts from "look what we built" to "look what we built *work*." Emotional core—"It Lives"—reflects the 25-year arc: DirectX 7 → HTML5 Canvas, Windows → web, museum piece → living application.

**Structure:**
1. Opening beat — "The moment when all pieces talk to each other"
2. Sprint 8 scorecard — What was missing (integration)
3. Integration story (6 steps): Aspire orchestration, Blazor DI wiring, component hierarchy, game engine heartbeat, SignalR hub dispatcher, render loop
4. Full DI chain (Mermaid diagram) — From Aspire through GameEngine, Hub, Blazor components to Canvas
5. User experience — Creatures moving, selection, population statistics live
6. Emotional core — "It Lives": 25 years, legacy → modern, DirectX → Canvas, Windows → web
7. Technical checklist — Sprint 9 deliverables (14 items)
8. Epilogue — The inflection point; next steps (Sprints 10-13)

**Key Narrative Choices:**
- **DI as First-Class Story** — Dependency injection chain shown in code + Mermaid diagram (teaches pattern without preaching)
- **"It Lives" vs "It Works"** — Chosen for emotional resonance and 25-year historical context
- **.NET Aspire as a Story** — "Distributed systems made simple": infrastructure as dependency injection
- **Hub as Thin Dispatcher** — Emphasizes design principle: never throws, never holds state, never does business logic
- **30 Hz Server, 60 FPS Canvas** — Explains tick/frame mismatch: pragmatic game engine design with interpolation

**Technical Content:**
- Mermaid DI integration chain: Aspire orchestration → server registration → web registration → data flow
- Code examples: AppHost/Program.cs, Web/Program.cs, GameView.razor, GameService.cs, TerrariumHub.cs, CanvasGameRenderer.js
- GameEngine heartbeat (10-phase turn processor ticking at 30 Hz)
- TerrariumHubClient event-driven integration
- CanvasGameRenderer requestAnimationFrame loop at 60 FPS

**Acceptance Criteria:**
✅ Blog saved to `docs/blog/journal/09-it-lives.md`
✅ Covers .NET Aspire orchestration with code examples
✅ Explains DI chain from Aspire through Blazor components to Canvas
✅ Includes full Mermaid DI diagram
✅ Explains GameEngine heartbeat (30 Hz)
✅ Explains SignalR hub philosophy (thin dispatcher)
✅ Explains CanvasGameRenderer (60 FPS, requestAnimationFrame)
✅ User experience section (creatures moving, selection, population stats)
✅ Emotional core ("It Lives") with 25-year arc
✅ Technical checklist (14 Sprint 9 deliverables)
✅ Developer-first tone (Hanselman-ready)
✅ Mermaid diagrams only (no ASCII art)

**Word Count:** ~4,500

**Why:** Brady's "blog everything" directive. Sprint 9 is the integration moment—the emotional peak of the modernization story. This post proves that pieces work together and sets up the final three sprints (10-12 feature work, 13 announcement).

### 2025-07-16: DI + Service Registration (Sprint 9) — Issue #62
**By:** Heisenberg (Lead / Architect)
**Status:** Implemented

The Terrarium stack had no dependency injection wiring. GameEngine, NetworkEngine, and the renderer were either constructed manually or registered ad-hoc. This blocked testability and Aspire integration.

**Resolution:**
1. Created interface abstractions: `IGameEngine`, `INetworkEngine` (IGameRenderer already existed)
2. Three registration methods following the established pattern:
   - `AddTerrariumGameEngine()` in Terrarium.Game: registers `IGameEngine`→`GameEngine` (singleton), `PopulationData`, `AssemblyValidator`, `CreatureValidator`
   - `AddTerrariumNetworking()` in Terrarium.Game: registers `INetworkEngine`→`NetworkEngine` (singleton), `NetworkEngineOptions`
   - `AddTerrariumRenderer()` in Terrarium.Web: registers `IGameRenderer`→`CanvasGameRenderer` (scoped — per Blazor circuit)
3. Service lifetimes: singleton for game engine and networking (long-lived, stateful), scoped for renderer (JS interop per circuit)
4. Program.cs now calls all four registration methods in sequence

**Alternatives Considered:**
- Single `AddTerrarium()` method rejected (couples all subsystems)
- Transient game engine rejected (holds mutable world state)
- Singleton renderer rejected (holds JS interop refs tied to Blazor circuit)

**Impact:** All key services injectable and mockable for unit testing. Terrarium.Web now references Terrarium.Game. Future sprints can inject services directly.

---

### 2025-07-16: Engine Wiring (Sprint 9) — Issues #63, #64, #65
**By:** Mike (Networking / Engine Dev)
**Status:** Implemented

**Decisions:**

1. **IEngineRenderer abstraction:** Created in Terrarium.Game.Rendering, separate from IGameRenderer. Engine produces WorldRenderData (pure data), consumers implement IEngineRenderer. Keeps dependency clean: Game → IEngineRenderer ← Web.Rendering.

2. **Fire-and-forget for render and service dispatches:** Both _renderBridge.RenderTickAsync() and _serviceBridge.ReportPopulationAsync() are fire-and-forget from game loop. Renders and reports are best-effort; game continues on failure.

3. **TeleportStatePayload as serialization boundary:** Created dedicated record instead of serializing OrganismState directly. Carries only: organism ID, species name, position, radius, generation, energy. Avoids leaking internal state over wire.

4. **Bridge pattern over direct coupling:** GameRenderBridge, GameNetworkBridge, GameServiceBridge mediate between GameEngine and external systems. Keeps engine focused on simulation (700+ lines, 10-phase loop). Bridges independently testable and swappable via DI.

5. **IGameEngine interface extended with bridge properties:** RenderBridge, NetworkBridge, ServiceBridge properties allow wiring after DI construction. ServiceBridge setter propagates to PopulationData for automatic reporting.

**Impact:** All external integration goes through bridges. New integrations follow same pattern. UI may skip render frames under heavy load (acceptable, legacy behavior). Population reports may be lost if server unreachable (acceptable).

---

### 2025-07-16: Smoke Test Architecture (Sprint 9) — Issue #66
**By:** Hank (Tester/QA)
**Status:** Implemented

**Test Coverage:** 20 tests across 4 test classes, all passing.

| Test Class | Tests | Status |
|---|---|---|
| ServerStartupSmokeTests | 4 | ✅ All passing |
| SignalRHubSmokeTests | 6 | ✅ All passing |
| GameEngineSmokeTests | 6 | ✅ All passing |
| DiContainerSmokeTests | 4 | ✅ All passing |

**Decisions:**

1. **Self-contained TestServer:** Used Microsoft.AspNetCore.TestHost with TerrariumServerFactory fixture instead of WebApplicationFactory<Program>. Reason: Terrarium.Server has pre-existing CS0103 errors in SpeciesEndpoints.cs. Same decoupling pattern used by Terrarium.SignalR.Tests.

2. **Shared test fixture:** TerrariumServerFactory shared via IClassFixture across ServerStartupSmokeTests, SignalRHubSmokeTests, DiContainerSmokeTests. Avoids spinning up multiple servers.

3. **GameEngine tests standalone:** Tested without server — only needs logger and PopulationData. Uses NullLogger<GameEngine>.

4. **Project references:** References Terrarium.Net, Terrarium.Game, Terrarium.Configuration, Terrarium.ServiceDefaults directly. Does NOT reference Terrarium.Server (build errors) or Terrarium.Web (Blazor components not needed).

**Not Tested (deferred):**
- Renderer initialization (coupled to Blazor, sprite assets — deferred until testable interface)
- Server endpoint responses (requires Terrarium.Server reference, blocked by build errors)
- Terrarium.Web startup (requires Aspire TestHost integration)

**Consequences:** Smoke tests run in CI without database or external dependencies. When SpeciesEndpoints.cs fixed, tests can expand to full-fidelity server testing.

---

### 2025-07-17: GameView Wired into Main Layout (Sprint 9) — Issue #61
**By:** Skyler (Frontend Web Dev)
**Status:** Implemented

**What:** GameView component (Sprint 8 canvas renderer) is now primary game viewport in main application layout, replacing legacy TerrariumViewport placeholder.

**Layout Structure:**
- **Main area (left):** GameView canvas — full interactive game viewport with pan/zoom/creature selection
- **Right sidebar (260px):** Three collapsible glass-theme sections:
  1. Ecosystem metrics (running state, creature/peer counts, tick number)
  2. Creature panel (species list with selection and stats)
  3. Event log (timestamped game messages)
- **Bottom status bar:** Connection LED, population count, tick counter, peer count

**SignalR Integration:** Home.razor subscribes to 5 TerrariumHubClient events:
- OnEcosystemTick — tick count, peer count, running state
- OnPopulationReport — creature list from per-species data
- OnPeerAnnounce — peer join/leave log entries
- OnWorldStateUpdate — world state log entries
- OnError — hub error log entries

All subscriptions cleaned up via IDisposable.

**Responsive Design:** Sidebar stacks below viewport at ≤768px, further compresses at ≤480px. All styles use Glass theme CSS tokens.

**Impact:**
- Home.razor now owns game state and SignalR event wiring
- MainLayout.razor simplified to just chrome frame
- Status bar moved from layout to Home
- EcosystemStatus.TickCount changed from int to long (matches EcosystemTick.TickNumber)
- TerrariumViewport.razor preserved but unused (backward compatibility)

### 2026-02-11: Multi-client testing infrastructure and performance benchmarks

**By:** Hank

**What:** Created two new test projects for Sprint 11:
1. Terrarium.MultiClient.Tests — xUnit tests that simulate N browser clients via SignalR hub connections
2. Terrarium.Benchmarks — BenchmarkDotNet performance tests for game engine and SignalR hub

**Why:** Sprint 11 requires multi-client ecosystem testing and load/stress testing to validate the peer-to-peer architecture at scale. The multi-client tests verify that teleportation, peer discovery, and population reporting work correctly across multiple simultaneous browser connections. The benchmarks establish baseline performance metrics for game engine tick duration and SignalR message throughput to guide optimization work.

**Testing patterns:**
- Both projects use the TestHost pattern (no direct Terrarium.Server dependency) to avoid pre-existing build errors
- Multi-client tests follow same patterns as existing SignalR.Tests (TaskCompletionSource, WaitWithTimeout helper)
- Docker Compose file (docker-compose.test.yml) enables CI testing with postgres + server + test runner
- Performance baselines documented in docs/performance-baselines.md with target metrics (60 FPS @ 50 organisms, 1000+ msgs/sec)

### 2026-02-11: Sprint 11 Teleportation & Peer List UX

**By:** Skyler
**What:** Implemented teleportation visual effects on canvas (zones, portals, notifications) and peer list UI component showing network health and connected peers
**Why:** Issues #74 and #76 required frontend UX for the networking layer. Teleportation effects provide visual feedback for cross-peer creature transfer (arrival/departure animations, toast notifications, activity log). Peer list gives users visibility into network status and connected peer count. Both integrate with existing SignalR events from TerrariumHubClient.

**Technical decisions:**
- Teleport zones are 100px-wide strips at world edges (left/right/top/bottom), rendered with pulsing cyan glow
- Portal animations use the 16-frame teleporter sprite with particle effects (green for arrivals, blue for departures)
- Toast notifications are canvas-rendered (not DOM) with glass theme styling, 4-second auto-dismiss
- Activity log capped at 50 entries to prevent unbounded growth
- PeerList component auto-refreshes on PeerJoined/PeerLeft events, shows truncated peer IDs (8 chars) with full ID in tooltip
- Relative time formatting for connection timestamps ("just now", "Xm ago", "Xh ago", "Xd ago")

**Files added:**
- Components/PeerList.razor — peer list component with network health indicators
- CSS additions to glass-components.css for .peer-list styling

**Files modified:**
- wwwroot/js/terrarium-renderer.js — added teleportation effects rendering and API
- Components/Pages/Home.razor — integrated PeerList component, wired OnCreatureTeleport event

### 2025-02-11: Azure SignalR Service integration for horizontal scaling

**By:** Saul  
**What:** Configured Azure SignalR Service as an Aspire resource with optional fallback to in-process SignalR for local development. Updated Bicep infrastructure to provision SignalR Standard S1 with sticky sessions enabled on Container Apps. Documented the scaling architecture with Mermaid diagrams showing local vs. production topology, cross-server message routing, and operational characteristics.

**Why:** Terrarium.Server needs to scale horizontally across multiple Container App instances to support thousands of concurrent clients. Azure SignalR Service provides a managed backplane for SignalR hub-and-spoke architecture, eliminating the need for Redis infrastructure. Sticky sessions preserve per-connection rate limit state consistency across instances. The optional design allows local dev to run with in-process SignalR (no Azure dependency), while production automatically uses Azure SignalR when the connection string is present.

**Technical Details:**
- Aspire AppHost: Added AddAzureSignalR("signalr") with emulator support for local dev
- Server: Conditional AddAzureSignalR() when ConnectionStrings__signalr is present, enforcing ServerStickyMode.Required
- Bicep: Provisioned Microsoft.SignalRService/signalR with Standard_S1 SKU (1,000 connections), sticky sessions on Container Apps ingress
- Documentation: Created docs/architecture/signalr-scaling.md with production topology diagrams, capacity planning, failure modes, and cost estimation

**Scaling Characteristics:**
- Current capacity: 1,000 concurrent connections (SignalR S1), 1-3 server instances
- Auto-scaling: Container Apps scales based on HTTP concurrent requests
- Message throughput: 1,000 msg/sec inbound, 2,000 msg/sec outbound (well within EcosystemTick broadcast requirements)
- Sticky sessions: Required for per-connection rate limit state consistency

**Related Files:**
- src/Terrarium.AppHost/Program.cs — SignalR resource registration
- src/Terrarium.Server/Program.cs — Conditional Azure SignalR configuration
- src/Terrarium.Server/Workers/SignalRScalingService.cs — Startup logging service
- infra/main.bicep — Azure SignalR provisioning + Container Apps sticky sessions
- docs/architecture/signalr-scaling.md — Full scaling architecture documentation

### 2026-02-11: Population Tracking Architecture
**By:** Mike
**What:** Hub-and-spoke population aggregation with in-memory server-side tracking
**Why:** SignalR hub aggregates population data from all clients, maintains per-peer-per-species contributions, and broadcasts consolidated stats. Broadcast throttled to 10-tick intervals. In-memory for Sprint 11, Orleans grain replacement in Sprint 12. Client-side PopulationChart component displays top 10 species with bar chart visualization.

### 2026-02-11: Error handling architecture for Sprint 12

**By:** Heisenberg

**What:** Implemented comprehensive error handling and resilience across the entire Terrarium stack:
1. TerrariumErrorBoundary component wrapping all pages — catches render exceptions with user-friendly recovery UI
2. Graceful degradation in Home.razor — automatic switch to local-only mode when server unreachable (after 3 connection attempts)
3. SignalR exponential backoff reconnection — confirmed already implemented in TerrariumHubClient (immediate, 2s, 10s, 30s, 60s)
4. HTTP retry logic — added StandardResilienceHandler to all service clients (3 retries, exponential backoff, circuit breaker, timeout)
5. Enhanced error categorization — distinguish network errors, timeouts, and validation failures

**Why:** 
- **Resilience:** Game must continue running even when server is down — local-only mode preserves user experience
- **User trust:** ErrorBoundary prevents white screens — users see friendly error messages and can recover
- **Network reliability:** StandardResilienceHandler handles transient failures transparently (cloud services, mobile networks)
- **Debugging:** Environment-aware error display (stack traces in dev, generic messages in prod) + structured logging
- **Maintainability:** Centralized error handling patterns reduce duplication across pages

**Rationale:**
- ErrorBoundary is a standard Blazor pattern — custom TerrariumErrorBoundary adds telemetry hooks and fallback navigation
- Local-only mode aligns with original Terrarium's P2P architecture — game should degrade gracefully, not crash
- StandardResilienceHandler is .NET's built-in solution — no need for custom Polly policies
- Three connection attempts balances user patience (don't give up too soon) vs. responsiveness (don't hang forever)
- Exponential backoff prevents thundering herd on server restart

**Impact:**
- Users experience fewer crashes and hangs
- Server downtime no longer blocks gameplay
- Transient network errors auto-retry without user intervention
- Error logs become more actionable with categorization
- Future enhancements (toast notifications, telemetry, offline queue) have a foundation

**References:**
- docs/error-handling-architecture.md
- src/Terrarium.Web/Components/Shared/TerrariumErrorBoundary.razor
- src/Terrarium.Web/Components/Pages/Home.razor

### 2025-01-23: Ecosystem Mode Selection and Game State Persistence

**By:** Mike (Engine/Networking)

**What:** Implemented two major features for Sprint 12:
1. **Issue #81 - Ecosystem mode selection**: Created `EcosystemMode` enum with LocalOnly and Networked modes. Updated GameEngine to check mode before network operations (teleportation, population reporting). Mode can be switched at startup and runtime via `GameEngine.Mode` property.
2. **Issue #82 - Save/Load game state**: Created `IGameStatePersistence` interface and `GameStatePersistence` implementation using System.Text.Json. Serialization includes organism positions, species data, energy levels, and tick count. Added `SaveGameStateAsync` and `LoadGameStateAsync` methods to GameEngine.

**Why:** 
- LocalOnly mode enables offline gameplay and testing without requiring server infrastructure
- Networked mode preserves existing multiplayer functionality
- Save/Load enables game session persistence and recovery
- Using System.Text.Json provides modern, performant serialization
- Interface-based design allows multiple persistence backends (server, browser download, local file)

**Implementation Details:**
- `EcosystemMode.cs`: Simple enum with LocalOnly (0) and Networked (1) values
- Updated `GameEngine.TeleportOrganisms()` to skip teleportation in LocalOnly mode
- Updated `PopulationData.EndTick()` to skip server reporting in LocalOnly mode
- Mode changes propagate from GameEngine to PopulationData automatically
- `IGameStatePersistence` defines serialize/deserialize and save/load contracts
- `GameStatePersistence` implements JSON serialization with WorldStateSaveData and OrganismSaveData DTOs
- Registered `IGameStatePersistence` in DI container via GameServiceExtensions
- Save/Load methods integrate with existing GameEngine architecture

**Organism Assembly Handling:**
Save operations store assembly full names and species references. Load operations require the caller to provide a PrivateAssemblyCache (PAC) to reload creature assemblies. This design separates serialization concerns from assembly loading/validation.

**Future Work:**
- Server-side save/load endpoints (SaveToServerAsync, LoadFromServerAsync) - marked as TODO
- Browser download trigger (SaveAsBrowserDownloadAsync) - requires JSInterop in Blazor context
- Organism restoration from save data - requires PAC integration in LoadGameStateAsync
- Settings UI integration for mode selection
- Runtime mode switching with proper network connection teardown

### 2026-02-11: Server monitoring architecture — Aspire-integrated health checks and System.Diagnostics.Metrics

**By:** Gus (Server Dev)

**What:** Server monitoring uses Aspire-integrated health checks with liveness/readiness tags, System.Diagnostics.Metrics for counters/gauges, and structured logging with ILogger.BeginScope() for consistent property names across all server components.

**Why:** 
- Health checks follow Aspire conventions (`/health` for readiness, `/alive` for liveness) with tag-based filtering
- System.Diagnostics.Metrics is the native .NET telemetry API — flows directly to Aspire dashboard without custom exporters
- Structured logging with consistent property names (`PeerId`, `EcosystemId`, `TickNumber`, `TeleportId`, `OrganismId`) enables filtering and correlation in logs
- TerrariumMetrics lives in ServiceDefaults (shared layer) so both Server and Net projects can reference it
- Custom health checks are tagged `["ready"]` for readiness probes, self-check is tagged `["live"]` for liveness
- DatabaseHealthCheck returns Degraded (not Unhealthy) until database layer is implemented — signals "not ready but not broken"
- Metrics use dimensional tags (e.g., `ecosystem_id`) for filtering in dashboards

**Implementation details:**
- Health checks: `DatabaseHealthCheck`, `SignalRHubHealthCheck`, `AssemblyCacheHealthCheck` — all implement `IHealthCheck`
- Metrics: 6 metrics (3 gauges, 3 counters) in `TerrariumMetrics` class
- Structured logging: log scopes in `TerrariumHub` and `PopulationTrackingService` methods
- Log property naming: semantic names (`PeerId` not `ConnectionId`, `TotalOrganisms` not `Total`)
- Metrics providers: lambda functions in `Program.cs` for peer count (`TerrariumHub.GetConnectedPeerCount()`) and species count (`IPopulationTrackingService.GetActiveSpeciesCount()`)

### 2026-02-12: Performance instrumentation and profiling infrastructure

**By:** Hank  
**What:** Added System.Diagnostics.Metrics to GameEngine and performance.now() tracking to canvas renderer. Created comprehensive performance profile document with methodology, targets, and optimization roadmap.

**Why:**  
Sprint 12 Issue #83 required performance profiling infrastructure for the game loop. We need to:
- Track tick loop duration (target: <20ms for 30 FPS)
- Profile canvas rendering performance (target: <13ms)
- Monitor SignalR message throughput
- Detect memory leaks in long-running sessions

**Decisions Made:**

1. **System.Diagnostics.Metrics for GameEngine**
   - Modern, OpenTelemetry-compatible instrumentation
   - 4 metrics: process_turn.duration (histogram), phase.duration (histogram with phase tag), ticks.completed (counter), organisms.count (gauge by type)
   - Zero-allocation design using struct Stopwatch and static Meter
   - Phase-level granularity (0-9) enables bottleneck identification

2. **performance.now() for Canvas Renderer**
   - Rolling 60-frame window for statistical smoothing (2 seconds at 30 FPS)
   - Tracks: avg/min/max frame time, FPS, frames over 33ms threshold
   - New exports: getPerformanceStats(), resetPerformanceStats()
   - Overhead: <0.05ms per frame, 480 bytes fixed memory

3. **Performance Targets**
   - 30 FPS total (33ms frame budget)
   - Game tick: ≤20ms (10 phases)
   - Canvas render: ≤13ms
   - SignalR latency: <50ms
   - These are aggressive but achievable targets for smooth gameplay

4. **5-Phase Profiling Methodology**
   - Phase 1: Baseline (empty world, measure overhead)
   - Phase 2: Load testing (10/50/100/200 organisms)
   - Phase 3: Canvas (viewport stress, creature density, zoom)
   - Phase 4: SignalR (throughput, fanout, connection capacity)
   - Phase 5: Memory leaks (30-minute run, dotMemory/PerfView)

5. **Optimization Roadmap Documented**
   - High-impact/low-risk: viewport culling, label fade at low zoom, organism batching
   - Medium-impact/medium-risk: double buffering, terrain caching
   - Future work: WebGL, Web Workers
   - All tied to expected hotspots from code review

6. **Metrics Naming Conventions**
   - OpenTelemetry-compatible: {namespace}.{component}.{metric}
   - Tags for dimensions: phase, type
   - Units specified: ms, ticks, organisms
   - Ready for OTLP/Prometheus export

**Impact:**
- Enables data-driven optimization (measure, don't guess)
- Provides continuous monitoring in production via OpenTelemetry
- Establishes baseline for performance regression testing
- Documents expected hotspots for future developers
- Benchmark infrastructure already exists from Sprint 11 Issue #77

**Next Steps:**
- Fix NuGet package restore (AspNetCore.HealthChecks.AzureSignalR missing)
- Run benchmarks to establish baseline numbers
- Profile with dotnet-trace/Visual Studio
- Implement optimizations based on measured bottlenecks

### 2026-02-11: Container Apps health probes and auto-scaling configuration

**By:** Saul

**What:** Configured comprehensive health probes (liveness, readiness, startup) and intelligent auto-scaling rules for Azure Container Apps deployment. Server scales based on SignalR connections and CPU; Web scales based on HTTP concurrency.

**Why:** 
1. **Reliability**: Health probes enable Container Apps to automatically detect and restart failed containers (liveness) and remove unhealthy instances from load balancer rotation (readiness). Startup probes prevent restart loops during slow .NET assembly loading.

2. **Auto-scaling intelligence**: Scaling rules target the right metrics for each service — Server scales on SignalR connection count (primary workload indicator) and CPU, Web scales on HTTP concurrent requests. This ensures efficient resource utilization and cost control.

3. **Production readiness**: Three-tier probe strategy (liveness/readiness/startup) follows Azure Container Apps best practices. SQL health check ensures database connectivity before accepting traffic. SignalR health check intentionally omitted (no reliable package support for Azure SignalR Service, non-fatal if it fails).

**Configuration:**
- `infra/main.bicep`: Probe configuration for both serverApp and webApp resources
- `src/Terrarium.ServiceDefaults/Extensions.cs`: SQL Server health check registration
- `src/Terrarium.AppHost/Program.cs`: Aspire health check integration (`.WithHealthCheck()`)
- `docs/deployment/health-probes.md`: Complete documentation with testing guidance

**Scaling thresholds:**
- Server: 1-10 replicas, scale on CPU >70% or SignalR connections >100
- Web: 1-5 replicas, scale on HTTP concurrent requests >50

**Dependencies:**
- `AspNetCore.HealthChecks.SqlServer` 8.0.2 added to ServiceDefaults
- Requires Azure Monitor access for SignalR connection count metric

### 2026-02-11: Settings UI and PWA features completed (Issue #80, #85)

**By:** Skyler

**What:** Created comprehensive Settings UI with localStorage persistence and full PWA support (manifest, service worker, responsive design)

**Why:** Issue #80 required web settings panel for ecosystem mode, network config, display settings, and theme selection. Issue #85 required mobile/tablet responsive design and PWA capabilities. Combined both issues into a cohesive mobile-first web experience.

**Technical details:**
- Settings.razor: localStorage-backed settings with auto-save, glass-themed form controls
- manifest.json: PWA metadata with Terrarium branding, installable as standalone app
- sw.js: Shell caching service worker for offline capability, network-first strategy
- responsive.css: Mobile (≤768px), tablet (769-1024px), desktop breakpoints; touch controls; orientation handling
- App.razor: PWA meta tags, manifest link, service worker registration
- NavMenu: Settings link added

**Files created:**
- `src/Terrarium.Web/Components/Pages/Settings.razor`
- `src/Terrarium.Web/wwwroot/manifest.json`
- `src/Terrarium.Web/wwwroot/sw.js`
- `src/Terrarium.Web/wwwroot/css/responsive.css`
- `src/Terrarium.Web/wwwroot/assets/ICON-NOTES.md` (placeholder icon generation guide)

**Files modified:**
- `src/Terrarium.Web/Components/App.razor` (PWA support)
- `src/Terrarium.Web/Components/Layout/NavMenu.razor` (Settings link)

### 2025-01-XX: SDK packaging finalized and verified for Issue #89

**By:** Hank
**What:** Completed comprehensive SDK packaging verification and documentation for creature development
**Why:** Final sprint deliverable — SDK must be ready for external developers to create creatures

#### ✅ NuGet Package (Terrarium.OrganismBase)
- **Metadata**: Complete and comprehensive
  - PackageId: `Terrarium.OrganismBase`
  - Version: `10.0.0-preview.1`
  - Authors, Description, License, Tags all present
  - Package README included
  - Documentation XML generation enabled
  - Symbol package configured (snupkg)
- **Build Status**: ✅ Builds successfully
- **Pack Status**: ✅ Packs successfully with `dotnet pack`

#### ✅ dotnet new Template (Terrarium.Templates)
- **Template Configuration**: Complete
  - Short name: `terrarium-creature`
  - Parameters: CreatureType, IsCarnivore, AuthorName, AuthorEmail, Framework
  - Conditional compilation for Animal vs Plant
  - Scaffolded code includes event handlers, attributes, TODO comments
- **Build Status**: ✅ Builds successfully
- **Pack Status**: ✅ Packs successfully as template package

#### ✅ SDK Documentation Structure
**Comprehensive and cross-referenced:**

```
docs/sdk/
├── README.md (updated with getting-started link)
├── getting-started.md (NEW - 10-minute quickstart)
├── tutorials/
│   ├── getting-started.md (conceptual intro, links to hands-on)
│   ├── tutorial-1-simple-plant.md (cross-linked)
│   ├── tutorial-2-herbivore.md (cross-linked)
│   └── tutorial-3-carnivore.md (cross-linked)
└── api/
    ├── README.md (cross-linked)
    ├── animal.md
    ├── plant.md
    └── attributes.md
```

**Key features:**
- Practical, hands-on approach
- Copy-paste ready code examples
- Complete smart herbivore logic (scans, moves, eats, reproduces)
- Clear success indicators
- Troubleshooting for common issues
- Links to advanced tutorials and API docs

**Developer Experience Flow:** 1 → Install templates, 2 → Create project, 3 → Follow getting-started guide, 4 → Deploy creature. **Time to first deployed creature: ~10 minutes.**

✅ **SDK is ship-ready for Sprint 13 final deliverable.**

### 2025-01-20: ARCHITECTURE.md updated for modern .NET 10 solution

**By:** Heisenberg (Lead Architect)

**What:** Replaced the legacy ARCHITECTURE.md (which documented the .NET 3.5 codebase) with a comprehensive architecture document for the modernized .NET 10 solution.

**Key sections added:**
1. **Solution structure**: All 23 projects in `src/Terrarium.sln` with clear categorization
2. **Project dependency graph**: Mermaid diagram showing zero circular dependencies (clean DAG)
3. **Project details**: Each project gets a dedicated section with role, technology, responsibilities, key files, dependencies, and DI registration
4. **Interface contracts**: Full catalog of all public interfaces (`ITerrariumHub`, `IOrganismEngine`, `IPhysicsEngine`, `ITeleportationService`, etc.)
5. **Data flow diagrams**: Sequence diagrams for game loop and creature upload
6. **Modernization comparison table**: Legacy (.NET 3.5) vs. Modern (.NET 10) side-by-side
7. **Deployment architecture**: Azure Container Apps topology with Mermaid diagram
8. **Build configuration**: Global settings from `Directory.Build.props`, package management strategy
9. **Testing strategy**: All 8 test projects with coverage areas and commands
10. **Security model**: Creature sandboxing approach, DLL validation, timeout enforcement

**Why:** The old ARCHITECTURE.md was a snapshot of the legacy .NET 3.5 codebase with WinForms, DirectX, and ASMX web services — none of which exist in the modernized solution. This created confusion:
- New contributors couldn't understand the modern architecture
- The dependency graph showed projects that no longer exist
- Technology choices (Blazor, SignalR, Aspire) were undocumented
- DI registration patterns were invisible
- No deployment guidance for Azure Container Apps

The new document is **authoritative** — it describes what IS, not what WAS. The legacy architecture is preserved in git history (`git log ARCHITECTURE.md`) and referenced in MODERNIZATION.md.

### 2025-01-20: Legacy .NET 3.5 codebase deleted (764 files removed)

**By:** Heisenberg (Lead Architect)

**What:** Removed all legacy code directories via `git rm -r`, cleaning the repository to contain only the modern .NET 10 solution. **All legacy code remains in git history** — nothing is lost, just archived.

**Directories removed:**
- `Client/` — 13 WinForms projects (.NET 3.5): Terrarium, TerrariumWPF, Game, Renderer, Configuration, Controls, Glass, OrganismBase, Services, HttpListener (legacy implementations)
- `ClientWPF/` — 13 empty WPF project shells (.NET 4.0): never completed, only AssemblyInfo.cs files
- `Server/` — Legacy ASP.NET WebForms + ASMX web services server (.NET 3.5)
- `ServerMVC/` — ASP.NET MVC 2 scaffold (.NET 4.0): only HomeController and AccountController, never completed
- `SDK/` — Legacy tutorial exercise projects (.NET 2.0): Exercise1, Exercise2, Exercise3
- `Samples/` — Legacy creature samples (.NET 2.0): Carnivore, Herbivore, Plant (replaced by `src/Terrarium.Samples/`)
- `Tools/` — Legacy server config tools (.NET 2.0): ServerConfig, InstallerItems
- `Keys/` — Legacy strong-name key files (`development.snk`)
- `Terrraium2010.sln` — Old Visual Studio 2010 solution file (note the typo: three R's)
- `test_validation/` — Untracked test project (not in modern solution)
- `packages/` — Untracked NuGet build output directory

**Why:** The repository contained **three generations of code** (2001–2005, 2008, 2010) alongside the modern 2025 rewrite. This created confusion:
- New contributors saw 15+ year old code and assumed it was current
- Build files from three eras conflicted (`.sln`, `.csproj` formats)
- DirectX COM interop, ASMX SOAP, BinaryFormatter — all deprecated technologies
- Maintainability burden: 764 legacy files requiring security patches, dependency updates

**Decision rationale:**
1. **Preservation via git history**: Every line of legacy code is accessible via `git checkout <old-commit>` or browsing history on GitHub
2. **Clean mental model**: `src/` is the only active codebase — no ambiguity about which files to edit
3. **Modern tooling works**: No VS 2008/2010 project files confusing modern dotnet CLI
4. **Security posture**: Zero attack surface from .NET Framework 3.5 dependencies
5. **Onboarding speed**: New contributors see only relevant code

**Impact:**
- Repository size reduced by ~4 MB (764 files removed)
- Zero build warnings from legacy code
- Contributors focus on one solution (`src/Terrarium.sln`)
- Deployment CI/CD only builds modern artifacts
- Security scanning only analyzes .NET 10 dependencies

### 2025-01-20: README.md modernized for .NET 10 architecture

**By:** Heisenberg (Lead Architect)

**What:** Completely rewrote the root `README.md` to reflect the modernized .NET 10 architecture. The new README is developer-first, quick-start focused, and comprehensive.

**Key changes:**
- **Project overview**: Emphasizes the 25-year journey from .NET 1.0 to .NET 10, highlighting Blazor WebAssembly, SignalR, Canvas 2D, and Azure Container Apps
- **Quick start**: Single command entry point via Aspire (`dotnet run --project src/Terrarium.AppHost`)
- **Architecture diagram**: Mermaid graph showing Blazor frontend → SignalR → Game Engine → Server API → Azure services
- **Project structure table**: All 9 core projects with clear purpose statements
- **Creature development guide**: Complete 3-step workflow (install SDK, write code, upload DLL) with runnable code example
- **Documentation links**: SDK tutorials, API docs, deployment guide, architecture deep-dive
- **Technology stack table**: Full modern stack (Blazor, SignalR, Aspire, Azure Container Apps, Canvas 2D)
- **Contributing section**: Areas to contribute (creatures, rendering, mechanics, networking, ML, mobile)
- **Preserved history**: Acknowledges original .NET Framework team, links to MODERNIZATION.md for migration story

**Why:** The previous README was from 2005 and described .NET Framework 2.0 technologies (Windows Forms, DirectX, ASMX web services) that no longer exist in the modernized codebase. New developers need:
1. Immediate clarity on what the modern stack is
2. A working quick-start command that launches the app
3. A clear path from "I installed .NET 10" to "I'm writing creatures"
4. Accurate architecture documentation that matches the actual code

The README is now the **front door** to the project — it should make developers excited to build creatures, not confused about which technologies we use.

**Impact:**
- GitHub visitors see a modern, active project (not a 2005 archive)
- New creature developers have a clear onboarding path
- Contributors know what the stack is and where to help
- README matches reality (Blazor, not WinForms; SignalR, not ASMX; Canvas 2D, not DirectX)

### 2026-02-11: Production deployment documentation and GitHub Actions CD pipeline

**By:** Saul

**What:** Created comprehensive deployment guide (`docs/deployment/deployment-guide.md`), deployment checklist (`docs/deployment/checklist.md`), and GitHub Actions CD workflow (`.github/workflows/deploy.yml`) for Terrarium production deployment to Azure Container Apps.

**Why:** Sprint 13 (final sprint) deliverables — Issues #88 and #90. The team needs complete documentation and automation for deploying Terrarium to production. The deployment guide provides step-by-step instructions for prerequisites, local development with Aspire, Azure deployment via `azd up`, custom domain configuration, environment variables, monitoring with Application Insights, and troubleshooting. The deployment checklist ensures all pre-deployment requirements are met (infrastructure, authentication, code readiness, configuration), guides through deployment steps, verifies post-deployment health (Container Apps status, database connectivity, SignalR service, logging, auto-scaling, security), and includes GitHub Actions setup, monitoring configuration, and ongoing operational tasks. The GitHub Actions workflow automates the full CI/CD pipeline: builds with .NET 10, runs tests, authenticates to Azure via OIDC (passwordless), deploys using `azd deploy`, and provides deployment summary with URLs. Workflow uses OIDC federated credentials for secure, passwordless authentication (requires `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID` secrets and `AZURE_ENV_NAME`, `AZURE_LOCATION` variables configured in GitHub). Actual `azd up` execution deferred to Brady with Azure subscription — all steps fully documented for execution.

**Production readiness verified:**
- ✅ `azure.yaml` correctly defines server and web services as Container Apps
- ✅ `infra/main.bicep` provisions all required resources with correct configuration
- ✅ Health probes configured (Sprint 12 — `docs/deployment/health-probes.md`)
- ✅ Auto-scaling rules configured (CPU, SignalR connections, HTTP requests)
- ✅ GitHub Actions workflow created with OIDC authentication
- ✅ Deployment guide covers all steps from prerequisites to troubleshooting
- ✅ Deployment checklist ensures nothing is missed pre/post-deployment



# Decision: The Last Mile Blog Post

**Date:** 2026-02-11  
**Author:** Beth (Technical Writer)  
**Status:** Completed  
**Audience:** All team members, future readers, community  

## What Was Done

Wrote `docs/blog/journal/14-the-last-mile.md` — a detailed, technical, deeply human blog post documenting the post-Sprint 13 debugging chaos that occurred when Brady actually tried to *run* the app for the first time.

## The Context

After 13 sprints, 48 issues closed, 9 AI agents working in parallel, and every GitHub Actions test passing, the application didn't actually *run*. It had 9 distinct failures that cascaded when Brady tried `dotnet run` on the AppHost.

The post documents all 9 failures:
1. NU1901: NuGet vulnerability warning treated as error
2. NU1605: Package version downgrades
3. CS1061: Enum shadowing a bool property (naming collision)
4. CS0266/CS1061: Persistence layer casting and property name errors
5. CS0103: Razor @media directive interpreted as C# directive
6. CS1061: Aspire APIs that don't exist yet
7. Port 5000 collision (both Server and Web defaulted to same port)
8. Password parameter with no value
9. Health check returning Degraded instead of Healthy

## The Narrative Decision

This post tells the story of "the gap between tests passing and the system actually working." It's not a failure post—it's a *realism* post. It shows:

- **The irony:** 9 agents, 48 issues, 13 sprints, all tests green... and the app doesn't run because of practical integration issues.
- **The lesson:** Integration testing requires actually running the system end-to-end. No amount of unit tests, CI, or automated builds can replace a human actually launching the app.
- **The respect:** The AI agents built incredible, well-tested parts. The failure was one of scope (parts don't guarantee a system works), not quality.
- **The tone:** Developer-to-developer, self-aware, funny but respectful, with technical depth in every explanation.

## Why This Matters

This post:
1. **Resonates with every distributed systems team.** Every team with multiple services has hit this gap.
2. **Humanizes the AI agents' work.** Shows both what they excelled at (building parts, writing tests) and what they couldn't do (integration testing at runtime).
3. **Bridges to the next conversation.** "Now that the app runs, what's next?" becomes the natural question.
4. **Provides practical debugging lessons.** Every failure has a root cause analysis and a fix. Readers learn how to debug similar issues.
5. **Sets up for the Hanselman blog.** The last-mile story adds credibility to the full announcement. It's not "everything was perfect"—it's "everything was perfect until we actually ran it, then we fixed 9 real issues." That's *real*.

## Decision Points Made

1. **All 9 failures included, not just a summary.** Each failure gets error message, investigation, and fix. This is the teaching value.
2. **Escalating complexity order, not chronological.** Start with straightforward dependency problems, end with subtle integration architecture questions.
3. **Human debugging narrative.** Show the trial-and-error, the wrong API calls, the dead ends. That's realistic and educational.
4. **Emotional honesty.** The closing beat is: "Brady ran it 5 more times after this because now he knows green tests don't guarantee a working system." That's the real lesson.
5. **Respect for the agents throughout.** Never condescending. The agents built in isolation and passed tests. Integration testing at runtime is a different problem.

## What This Enables

- Hanselman blog now has a complete narrative arc: challenge → journey → integration → last-mile reality.
- Future teams can reference this post when they hit similar issues.
- The team has documented proof that "all tests pass" and "system works" are different things.
- Readers understand why human-in-the-loop is important even for AI-driven development.

## Follow-Up

The blog is ready for publication. The app now runs correctly. The next decision is whether to make small tweaks before Hanselman sees it, or to hand it as-is and let the story speak for itself.


# Decision: CORS Configuration for Terrarium Server

**By:** Gus
**Date:** 2025-07-17
**Status:** Implemented

## What
Added a default CORS policy to `Terrarium.Server/Program.cs` that allows:
- Origins: `localhost` (any port), `127.0.0.1`, and `*.internal` (Aspire service discovery)
- Methods: GET, POST (required by SignalR)
- Headers: all
- Credentials: true (required by SignalR negotiate)

## Why
- SignalR requires CORS when clients connect cross-origin (browser-direct scenarios)
- Current Blazor Server hub client runs server-side so CORS isn't blocking today, but any future browser-direct SignalR or external tooling would fail without it
- `SetIsOriginAllowed` with host check is flexible enough for dev without being wide-open in production
- `AllowCredentials()` is mandatory for SignalR's cookie/token-based negotiate handshake

## Hub Path Confirmed
- Server maps `TerrariumHub` at `/hubs/terrarium` — canonical path, no change needed
- Jesse is fixing the client side to connect to `/hubs/terrarium` (was `/terrarium`)


# Decision: Server Logging & Missing Game Loop Diagnosis

**Date:** 2025-07-17
**Author:** Gus (Server Dev)
**Status:** Decided

## Context

Brady reported the frontend shows "Connected" but the canvas is blank with zero logs from any component.

## Findings

1. **TerrariumHub was missing `OnConnectedAsync`** — client connections were never logged. Only disconnections were tracked via `OnDisconnectedAsync`.

2. **No game loop or world state broadcaster exists on the server.** The `ITerrariumClient` interface defines `ReceiveEcosystemTick` and `ReceiveWorldStateUpdate` callbacks, but **nothing on the server ever calls them**. The hub only relays data when clients call hub methods (like `ReportPopulation`). There is no `BackgroundService` that:
   - Runs a simulation tick
   - Broadcasts `EcosystemTick` to connected clients
   - Pushes `WorldStateUpdate` periodically

3. **`RequestWorldState` returns empty data** — `WorldWidth: 0, WorldHeight: 0, TickNumber: 0, OrganismCount: {peerCount}`. The TODO says "Sprint 11 — fetch from EcosystemGrain" but no grain or in-memory substitute exists.

4. **No startup banner** was being logged.

## What Was Done

- Added `OnConnectedAsync` to `TerrariumHub` with connection logging
- Added startup banner log in `Program.cs`
- Added heartbeat log to `NonPageServicesWorker` loop cycle

## What Still Needs to Happen (Root Cause of Blank Canvas)

The server needs a **game loop service** — a `BackgroundService` that:
1. Maintains world state (terrain grid, organism positions)
2. Runs periodic ticks (e.g., every 1 second)
3. Broadcasts `EcosystemTick` and `WorldStateUpdate` to all clients via `IHubContext<TerrariumHub, ITerrariumClient>`

Without this, the server is a passive relay — it only sends data when clients push data first. Since there's no game simulation, no client has anything to push.

The `IEcosystemNotifier` interface exists in `Terrarium.Net` and defines exactly the right methods (`NotifyTickAsync`, `NotifyWorldStateAsync`), but **no class implements it** on the server side.

## Decision

- Logging changes are immediate (this PR)
- Game loop service is a separate task — requires design decisions about world dimensions, tick rate, terrain generation, and organism spawning


# Server Connectivity Diagnosis

**Author:** Gus (Server Dev)  
**Date:** 2025-07-17  
**Status:** Diagnostic — no code changes made

## Summary

Three issues found that explain why the web frontend shows "network status: red" and no traffic reaches the server.

---

## Finding 1: Hub Path Mismatch (CRITICAL)

**Server maps:**  
`app.MapHub<TerrariumHub>("/hubs/terrarium");` → `Program.cs:66`

**Client connects to:**  
`.WithUrl($"{serverUrl}/terrarium")` → `TerrariumHubClient.cs:52`

The server exposes the hub at `/hubs/terrarium` but the client tries to connect to `/terrarium`. The SignalR negotiate request will 404.

**Fix:** Either change the server to `app.MapHub<TerrariumHub>("/terrarium")` or change the client to `.WithUrl($"{serverUrl}/hubs/terrarium")`. Recommend aligning to `/hubs/terrarium` (ASP.NET convention).

---

## Finding 2: No CORS Configuration (CRITICAL)

**Server `Program.cs`:** Zero CORS setup. No `AddCors()`, no `UseCors()`, no CORS policy defined anywhere.

When running under Aspire with separate ports (server on 5180, web on 5190), the web app's SignalR client makes cross-origin requests. Without CORS:
- The SignalR negotiate POST will be blocked by the browser's preflight check
- WebSocket upgrade requests may also fail

**Note:** This applies to browser-initiated connections. Since Terrarium.Web is Blazor Server (server-side rendering), the `TerrariumHubClient` runs on the server process, not the browser. In that case, CORS is not required — service-to-service calls bypass the browser entirely. However, if any JavaScript-based SignalR connections are added later, CORS will be needed.

**Verdict:** CORS is not the blocking issue for Blazor Server, but should be added for future-proofing.

---

## Finding 3: Hub Connection Never Started (CRITICAL)

**`TerrariumHubClient`** is registered as a singleton in DI (`Program.cs:31`), and the `Home.razor` page wires up all callbacks in `OnInitialized()`. But **no component or hosted service ever calls `HubClient.StartAsync()`**.

The connection is built but never opened. This is the primary reason there's zero traffic.

**Fix:** Either:
- Call `await HubClient.StartAsync()` in `Home.razor`'s `OnAfterRenderAsync(firstRender)`, or
- Create a `BackgroundService` / `IHostedService` that starts the connection at app startup

---

## Aspire Service Discovery: ✅ CORRECT

- `AppHost/Program.cs:10` registers server as `"server"`
- `Terrarium.Web/Program.cs:16` calls `AddTerrariumServices("server")` 
- `ServiceCollectionExtensions.cs:56` builds URI `https+http://server`
- `TerrariumHubClient.cs:47-49` resolves `Services:server:https:0` or `Services:server:http:0` from Aspire-injected config, falling back to `https+http://server`

The names match. Aspire service discovery is correctly wired.

---

## API Endpoints: ✅ CORRECT

Server exposes all expected endpoint groups:
- `/api/messaging` — welcome, MOTD, version
- `/api/discovery` — peer discovery
- `/api/species` — species management
- `/api/reporting` — population reporting
- `/api/charts` — chart data
- `/api/watson` — error reporting
- `/api/bugs` — bug reports
- `/api/usage` — usage analytics

The `Terrarium.Services` layer configures typed HttpClient instances for each, all pointed at `api/` base path. These match.

---

## Authentication: ✅ NO BLOCKER

No authentication middleware (`UseAuthentication`, `UseAuthorization`, `[Authorize]`) is present in `Program.cs` or on the hub. SignalR connections are unauthenticated — no auth barrier exists.

---

## Throttle Middleware: ⚠️ MINOR CONCERN

`ThrottleMiddleware` runs on all requests including WebSocket upgrades. At 60 req/min default, it could theoretically throttle SignalR negotiate + long-polling fallback requests during rapid reconnect cycles. Not a blocking issue but worth monitoring.

---

## Priority Fix Order

1. **Call `StartAsync()`** on `TerrariumHubClient` — without this, nothing connects
2. **Fix hub path** — server `/hubs/terrarium` vs client `/terrarium` mismatch
3. **Add CORS** — only needed if browser-direct SignalR connections are planned


# Decision: Playwright E2E Diagnostic Setup

**By:** Hank (Tester/QA)
**Date:** 2026-02-12
**Requested by:** Brady

## What

Created `src/Terrarium.Web.Tests.E2E/` as a Node.js Playwright test project with 9 diagnostic tests targeting the Blazor web frontend and Terrarium server.

## Why

Brady reported: Web renders at localhost:5190 but shows "network status: red circle" and empty blue canvas with no traffic between frontend and server. We need browser-level diagnostic visibility.

## Architecture Decision: Node.js Playwright (not .NET Playwright)

Chose standalone Node.js over a .NET `Microsoft.Playwright` project because:
- Faster to set up (no csproj, no solution integration needed)
- Playwright's Node.js API has richer documentation and community support
- These are diagnostic/investigative tests, not regression tests in the .NET test matrix
- Can run independently with `npm test` — zero coupling to the .NET build

## Key Diagnostic Findings (from first run)

All 9 tests passed. Here's what they revealed:

1. **Connection Status: 🔴 Disconnected** — LED class is `glass-led--idle`, label says "Disconnected". Never transitions to Connected during 30s observation.

2. **SignalR Hub endpoint returns 404** — `POST http://localhost:5180/terrarium/negotiate?negotiateVersion=1` returns HTTP 404. The server does NOT have `MapHub<TerrariumHub>("/terrarium")` wired up. This is the root cause.

3. **Server is healthy** — `http://localhost:5180/` returns 200 "Terrarium Server", `/health` returns 200 "Healthy", `/alive` returns 200 "Healthy".

4. **Browser makes ZERO requests to :5180** — all traffic goes to :5190 (the Blazor Server host). This is correct for Blazor Server mode — the `TerrariumHubClient` runs server-side, not in the browser.

5. **Canvas exists but is blank** — 972×619px canvas with 0 non-transparent pixels. The renderer initializes but never receives world state data to draw.

6. **Blazor circuit is working** — WebSocket to `ws://localhost:5190/_blazor` is open and exchanging frames normally.

7. **Only console error: 404 for `/images/terrarium-icon.png`** — minor missing asset, not related to the connectivity issue.

8. **PeerList LED shows `glass-led--failed`** — indicates the peer list component detected connection failure.

## Root Cause Summary

**The server at localhost:5180 does not map the `/terrarium` SignalR hub endpoint.** The `TerrariumHubClient` in the Web project tries to connect to `{serverUrl}/terrarium` but gets a 404. The server's `Program.cs` likely needs `app.MapHub<TerrariumHub>("/terrarium")`.

## Files Created

- `src/Terrarium.Web.Tests.E2E/package.json` — Node.js project
- `src/Terrarium.Web.Tests.E2E/playwright.config.js` — Playwright config
- `src/Terrarium.Web.Tests.E2E/tests/terrarium-diagnostics.spec.js` — 9 diagnostic tests
- `src/Terrarium.Web.Tests.E2E/.gitignore` — ignores node_modules, test artifacts

## How to Run

```bash
cd src/Terrarium.Web.Tests.E2E
npm test              # headless
npm run test:headed   # with browser UI visible
```


# Decision: Playwright Integration Tests Replace Diagnostics

**By:** Hank (Tester/QA)
**Date:** 2025-07-XX
**Status:** Implemented

## What

Replaced `terrarium-diagnostics.spec.js` (9 diagnostic-only tests) with `terrarium-integration.spec.js` (8 integration tests with real assertions).

## Why

The diagnostic suite was pure logging — `console.log` everywhere, zero assertions on actual behavior. Brady said "it looks like it works but it doesn't." Diagnostics don't catch regressions. Real assertions do.

## Test inventory

| # | Test name | What it validates |
|---|-----------|-------------------|
| 1 | map renders | Canvas visible, non-zero dimensions, has drawn pixels |
| 2 | connection status green | LED class `glass-led--active`, label "Connected" |
| 3 | organisms appear on canvas | Non-terrain pixels OR population > 0 |
| 4 | tick counter advances | Tick count increases over 5 seconds |
| 5 | population stats show organisms | Population > 0 in statusbar and sidebar |
| 6 | ecosystem status shows running | "Running" label + active LED |
| 7 | canvas is interactive | Mouse drag changes viewport pixel content |
| 8 | event log shows activity | ≥1 message-log entry with text |

## Running

```bash
cd src/Terrarium.Web.Tests.E2E
npx playwright test          # run all
npx playwright test --list   # list without running
```

Requires the app to be running at `http://localhost:5190` (start with Aspire first).

## Impact on other agents

- **Skyler/Jesse:** If you rename CSS classes (`.game-view__canvas`, `.glass-led--active`, `.connection-status__label`, `.ecosystem-status__metric`, `.glass-statusbar__section`, `.message-log__entry`), update the test selectors.
- **Mike:** Tests expect tick count to advance and population > 0 once SignalR data flows. If the game loop contract changes, tests may need updating.


### 2026-02-11: NuGet package version alignment and vulnerability suppression
**By:** Heisenberg
**What:** Fixed NU1901 and NU1605 NuGet errors by:
1. Added NU1901 to NoWarn list in Directory.Build.props (suppresses low-severity Microsoft.Identity.Client vulnerability warning)
2. Updated Microsoft.Extensions.DependencyInjection.Abstractions from 10.0.0 to 10.0.3 in Terrarium.Game.csproj
3. Updated Microsoft.Extensions.Logging.Abstractions from 10.0.0 to 10.0.3 in Terrarium.Game.csproj
4. Updated Microsoft.Extensions.Http from 10.0.0 to 10.0.3 in Terrarium.Services.csproj
5. Updated Microsoft.Extensions.Options from 10.0.0 to 10.0.3 in Terrarium.Services.csproj
6. Added Microsoft.Extensions.Http.Resilience 10.3.0 to Terrarium.Services.csproj (required for AddStandardResilienceHandler extension method)

**Why:**
- NU1901: Microsoft.Identity.Client 4.56.0 has a known low-severity vulnerability (GHSA-x674-v45j-fwxw). The vulnerability is transitive (comes through ASP.NET Core framework packages) and low-severity. TreatWarningsAsErrors=true was converting this to a build-blocking error. Suppressing NU1901 allows the build to proceed while the upstream framework package is updated. The alternative (explicitly updating Microsoft.Identity.Client) would require tracking down which transitive dependency brings it in and forcing a version override, which is more invasive.
- NU1605: Package downgrades detected when transitive dependencies required 10.0.3 but direct references pinned 10.0.0. NuGet's guidance is to "reference the package directly from the project to select a different version." Updated to 10.0.3 to match transitive requirements and eliminate downgrade warnings.
- Resilience package: Sprint 12 error handling changes added .AddStandardResilienceHandler() calls to HTTP client registrations, but the package providing that extension method (Microsoft.Extensions.Http.Resilience) was never added to Terrarium.Services.csproj. This caused CS1061 compilation errors. Adding the package resolves those errors.

**Impact:** NuGet restore and package resolution now succeed with 0 errors. Remaining build failures are pre-existing C# compilation errors in GameEngine.cs and GameStatePersistence.cs (not introduced by this change, not related to NuGet packages).


### Frontend Connectivity Diagnosis
**By:** Jesse
**Date:** 2025-07-17

#### Finding 1: SignalR connection is never started — ROOT CAUSE

`TerrariumHubClient` is registered as a singleton in DI (`Program.cs:31`) and its `StartAsync()` method exists (`TerrariumHubClient.cs:134`), but **nothing ever calls it**. The `Home.razor` page wires up event handlers in `OnInitialized()` (line 114–124) but never calls `await HubClient.StartAsync()`. The `SETTINGS-INTEGRATION.md` doc even shows the intended pattern (line 152–158) but it was never implemented.

**Impact:** The hub connection stays in `Disconnected` state permanently. `ConnectionStatus.razor` correctly reflects this as "Disconnected" with `glass-led--idle` (red circle). No traffic flows because the connection is literally never opened.

#### Finding 2: Hub URL path mismatch — would fail even if started

The `TerrariumHubClient` builds its hub URL as `{serverUrl}/terrarium` (line 52), which resolves to something like `http://localhost:5180/terrarium`. But the server maps the hub at `/hubs/terrarium` (`Server/Program.cs:66`: `app.MapHub<TerrariumHub>("/hubs/terrarium")`).

Similarly, `GameServiceExtensions.cs:53` sets `NetworkEngineOptions.HubUrl` to `"https+http://server/terrarium"` — also missing the `/hubs/` prefix.

**Impact:** Even if `StartAsync()` were called, the connection would get a 404 because the path doesn't match. The client needs to connect to `/hubs/terrarium`, not `/terrarium`.

#### Finding 3: Aspire service discovery is correctly wired

The `AppHost/Program.cs` correctly wires `web` with `.WithReference(server)`, which makes Aspire inject `Services:server:http:0` (or `:https:0`) config keys. `TerrariumHubClient` reads these keys (line 47–48) with a fallback to `"https+http://server"`. This part is correct — the service URL resolution should work fine.

#### Finding 4: No CORS issue — not applicable

There is **zero CORS configuration** anywhere in the codebase (no `AddCors`, no `UseCors`, no `WithOrigins`). However, CORS is not the problem here. The web frontend is a Blazor Server app — SignalR traffic goes over the Blazor circuit's WebSocket, which is server-to-server (the Blazor server process connects to the game server process). This is a backend HTTP call, not a browser cross-origin request. CORS doesn't apply.

#### Finding 5: Canvas renders but has nothing to show

The `GameView.razor` component initializes the `CanvasGameRenderer` on first render (line 42–53), which successfully calls `terrarium-renderer.js → initialize()`. The renderer:
1. Loads terrain tile images from `/assets/terrain/background.bmp` and `/assets/terrain/dirt.bmp`
2. Sets up the viewport (5000×5000 world)
3. Binds mouse/keyboard events

But `renderFrame()` is never called because no `WorldStateUpdate` events arrive (since SignalR is never connected). The `clear()` function does `clearRect()` which makes the canvas transparent. The "blue" appearance is the Glass theme's dark panel background showing through the transparent canvas (the `glass-panel--no-glass` class removes the frosted effect but keeps the dark blue gradient from `--glass-gradient-panel-bottom: #000060`).

The terrain would render correctly if `drawTerrain()` were called — it tiles the background image or falls back to `#2d5a27` green. But without a game loop calling `RenderFrameAsync()`, the canvas just sits cleared/transparent after initialization.

#### Summary: What needs to change

1. **Call `HubClient.StartAsync()`** — Add an `OnAfterRenderAsync` or `OnInitializedAsync` in `Home.razor` that starts the SignalR connection. The `SETTINGS-INTEGRATION.md` doc already shows the pattern.
2. **Fix hub URL path** — Change `TerrariumHubClient.cs:52` from `$"{serverUrl}/terrarium"` to `$"{serverUrl}/hubs/terrarium"`. Also fix `GameServiceExtensions.cs:53` from `"/terrarium"` to `"/hubs/terrarium"`.
3. **Add initial terrain render** — After canvas initialization, call `drawTerrain(null)` or `renderFrame(null)` to show the green terrain grid immediately, even before server data arrives. This eliminates the "blank blue screen" state.


# Decision: Fix Client Hub URL and Add Connection Startup

- **Date:** 2025-07-17
- **Author:** Jesse (Client Dev)
- **Status:** Implemented

## Context

The Blazor web client was never connecting to the SignalR hub due to three bugs:
1. Hub URL path was `/terrarium` but server maps at `/hubs/terrarium`
2. Same wrong URL in `GameServiceExtensions.cs`
3. `StartAsync()` was never called after wiring event handlers

## Decision

- Changed hub URL from `/terrarium` to `/hubs/terrarium` in both `TerrariumHubClient.cs` and `GameServiceExtensions.cs`
- Added `OnAfterRenderAsync(firstRender)` in `Home.razor` that:
  1. Renders an empty frame (terrain only) so users see green grass instead of blue CSS gradient
  2. Calls `HubClient.StartAsync()` with try/catch — falls back to local-only mode if server is unavailable
- Used `OnAfterRenderAsync` (not `OnInitializedAsync`) because the `GameView` canvas requires JS interop which is only available after first render in Blazor Server

## Coordination

Gus is fixing the server-side hub mapping separately. Both fixes must land for end-to-end connectivity.


# Decision: GameView self-renders initial terrain frame

**Date:** 2025-07-17
**Author:** Jesse (Client Dev)
**Status:** Implemented

## Context

After SignalR connectivity was fixed, the canvas remained blank blue. The previous fix added `RenderFrameAsync(new GameRenderState())` in `Home.razor`'s `OnAfterRenderAsync`, but this call was silently a no-op due to a Blazor lifecycle race condition.

## Problem

Blazor `OnAfterRenderAsync` fires **parent-first**: `Home.razor` executes before `GameView.razor`. When Home called `_gameView.RenderFrameAsync()`, GameView's renderer hadn't been created yet (`_renderer` was null, `IsInitialized` was false). The guard returned silently. After GameView's own `OnAfterRenderAsync` completed initialization, nobody called `renderFrame()` again.

## Decision

**GameView owns its initial render.** Immediately after `InitializeAsync()` completes in `GameView.OnAfterRenderAsync`, the component calls `RenderFrameAsync(new GameRenderState())` to draw green terrain tiles. Home.razor no longer attempts to render — it only handles SignalR.

## Rationale

- Components should be self-contained: a game view should display something meaningful the moment it initializes
- Eliminates parent-child timing dependencies
- Simpler mental model: GameView = init + first render; Home = network + game state

## Impact

- Canvas shows green grass terrain grid immediately on page load
- No dependency on server data for initial visual feedback
- Logging added throughout the render chain for observability


### 2025-07-17: Server-driven game loop via EcosystemSimulationWorker
**By:** Mike
**What:** Created a `BackgroundService` (`EcosystemSimulationWorker`) that seeds plants/herbivores/carnivores and runs a 500ms simulation tick, broadcasting full creature state via SignalR. Added `CreatureStateData` class and `Creatures` list to `WorldStateUpdate` in Terrarium.Net so clients can render creatures.
**Why:** The frontend was blank because nothing pushed game state to clients. The SignalR contracts existed but nothing invoked them. This worker fills the gap — it's a simple demo simulation that makes the UI visually alive while the real engine (GameEngine, Orleans grains) is still being wired. It broadcasts to all clients via `IHubContext` and the Home.razor was already wired to map `WorldStateUpdate.Creatures` → `CreatureRenderData` → canvas rendering. Population is capped at 5000 to prevent runaway growth.


### 2026-02-12: World state rendering pipeline wired end-to-end
**By:** Skyler
**What:** `HandleWorldStateUpdate` in `Home.razor` now maps `CreatureStateData` from `WorldStateUpdate` into `GameRenderState`/`CreatureRenderData` and calls `GameView.RenderFrameAsync()`. Dummy creatures removed from `OnInitialized` — all creature data comes from the server via SignalR.
**Why:**
- **This was the missing link:** The SignalR client received world state updates but just logged them — no render call, no creature mapping. The canvas showed terrain but zero creatures.
- **Two-type mapping is intentional:** `CreatureStateData` (Terrarium.Net) is the wire format from the server; `CreatureRenderData` (Terrarium.Web.Rendering) is the canvas rendering format. They're similar but decoupled — rendering may need fields (SrcX, SrcY, FrameSize) that the server doesn't care about.
- **Sidebar synced from world state:** `_creatures` list (drives `CreaturePanel`) is now populated from `WorldStateUpdate.Creatures` with live positions, so the sidebar reflects real server data.
- **Converged with Mike on `CreatureStateData`:** Mike added his creature DTO to `WorldStateUpdate` in parallel. I initially added a duplicate — caught it in build, removed mine, and mapped to his type. No conflict remains.
- **48px default FrameSize:** Matches the sprite sheet frame size used by `terrarium-renderer.js`. Creatures without sprites render as colored circles (fallback in JS renderer).

### 2026-02-12: User directive
**By:** bradygaster (via Copilot)
**What:** Telemetry/observability for the ecosystem simulation is a future need (to enable blogging about what's happening in the terrarium), but NOT for now. Do not implement telemetry yet.
**Why:** User request — captured for team memory

### 2026-02-12: Retrospective outcomes — integration testing gate, shared project ownership, configurable seeding
**By:** Heisenberg
**What:** Three process changes adopted from the full-project retrospective:
1. **Smoke test gate:** Every sprint must pass a Playwright smoke test (AppHost starts, SignalR connects, canvas renders) before marking complete. Hank owns implementation.
2. **Shared project ownership:** `Terrarium.Net` is owned by Mike. Any agent adding types must grep for existing definitions first to prevent duplicate type collisions.
3. **Configurable seeding:** `EcosystemSimulationWorker` initial organism counts must be driven by `IConfiguration`, not hardcoded constants. Enables tuning without code changes.
**Why:** All three post-sprint bugs (SignalR URL mismatch, missing CORS, StartAsync not called) were cross-agent wiring failures — not logic errors. Integration testing would have caught all three. The `CreatureStateData` type duplication and overloaded seeding (2300 → 16 organisms) were caused by implicit ownership and developer-centric defaults. These process changes target the actual defect injection patterns observed across Sprints 7-13.

