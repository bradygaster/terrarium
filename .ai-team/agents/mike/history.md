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
📌 Team update (2025-07-16): Solution uses classic .sln format (not .slnx); CS1591 suppressed during initial port — decided by Heisenberg
📌 Team update (2025-07-15): CSS tokens use `--glass-{category}-{element}-{modifier}` naming; BEM classes; `glass-theme.css` is single source of truth — decided by Jesse
