# Decisions

> Team decisions that all agents must respect. Append-only — never edit existing entries.

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

### 2026-02-10: Keep ArrayList Scan() for Now
**By:** Mike (Networking / Engine Dev)
**Status:** Proposed

**What:** The legacy `IAnimalWorldBoundary.Scan()` method keeps `ArrayList` as its return type for now. When the Game project is ported, change to `List<OrganismState>`.
**Why:** Changing to generic `List<OrganismState>` now would create a compile-time dependency on the unported Game project. Preserves source compatibility with existing creature code.

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


