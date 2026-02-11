# Project Context

- **Owner:** bradygaster (bradyg@microsoft.com)
- **Project:** .NET Terrarium 2.0 — peer-to-peer networked creature ecosystem game, modernized as a cross-platform web app
- **Stack:** C#, .NET 10, Blazor, ASP.NET Core, .NET Aspire, SignalR, Canvas/WebGL
- **Created:** 2026-02-10

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

📌 Team update (2026-02-11): Beth's voice is fearless, developer-first, community-driven — decided by bradygaster

📌 Team update (2026-02-11): Diagram standards — Mermaid only, no ASCII art. All diagrams must use Mermaid — decided by Badger, bradygaster
📌 Team update (2026-02-11): VB.NET respectful framing — never refer to VB.NET negatively, just "we're C# now" — decided by bradygaster
📌 Team update (2026-02-11): Orleans + SignalR hybrid recommended — 4 grain types for networking layer — decided by Heisenberg
📌 Team update (2026-02-11): CI pipeline created (.github/workflows/build.yml) targeting src/Terrarium.sln — decided by Hank
📌 Team update (2026-02-11): CSS token naming: --glass-{category}-{element}-{modifier}, BEM for components — decided by Jesse
📌 Team update (2026-02-11): SignalR Hub contract locked: ITerrariumHub (8 methods), ITerrariumClient (7 callbacks), rate limiting, error struct ReceiveError, 512KB message size limit — decided by Mike
📌 Team update (2026-02-11): Terrarium.Web Blazor project structure finalized (PR #118), components ready for SignalR integration, canvas via ElementReference — decided by Skyler
📌 Team update (2026-02-11): Glass CSS expanded to 60+ new tokens, all 76 original assets cataloged and available in wwwroot/assets/ — decided by Jesse
📌 Team update (2026-02-11): Organism Isolation architecture (3 layers: CreatureValidator, OrganismSandbox, OrganismHost+OrganismScheduler) replaces AppDomain — decided by Heisenberg
📌 Team update (2026-02-11): Hub-and-spoke SignalR architecture finalized: heartbeat 30s client/90s server, reconnect=rejoin, group per ecosystem — decided by Heisenberg

## Sprint 3 Work

### 2026-02-11: Created Terrarium.Web Blazor Interactive Server project (#22, #24)
- **Branch:** `squad/22-blazor-web`
- **PR:** #118
- **What I built:**
  - Full Blazor Interactive Server project scaffold in `src/Terrarium.Web/`
  - `Program.cs` with AddServiceDefaults, AddSignalR, MapRazorComponents + InteractiveServerRenderMode
  - `App.razor` referencing glass-theme.css, glass-components.css, app.css
  - `Routes.razor` with MainLayout default
  - `MainLayout.razor` — Glass chrome layout (titlebar + viewport body + statusbar with LED)
  - `_Imports.razor` with standard Blazor usings
  - Four reusable Blazor components:
    - `TerrariumViewport.razor` — Canvas-based game viewport placeholder
    - `CreaturePanel.razor` — Sidebar with creature stats (species, energy, age, position)
    - `EcosystemStatus.razor` — Header metrics bar with LED indicators (creature count, tick, peers)
    - `MessageLog.razor` — Scrollable timestamped event log
  - `app.css` with layout styles using Glass tokens throughout
  - Updated AppHost to reference and serve Terrarium.Web with external HTTP endpoints
  - `.csproj` references ServiceDefaults
- **Build:** `dotnet build src/Terrarium.sln` — all green
- **Glass theme integration:** All components use `--glass-*` CSS tokens and `.glass-*` BEM classes from Jesse's theme system. No hardcoded colors.

### 2026-02-11: Created SignalR client service for Blazor (#51, Sprint 7)
- **What I built:**
  - `Services/TerrariumHubClient.cs` — Full SignalR client service connecting to TerrariumHub
    - Handles all 7 ITerrariumClient callbacks as C# events (ReceiveEcosystemTick, ReceiveWorldStateUpdate, ReceiveCreatureTeleport, ReceivePeerAnnounce, ReceivePeerList, ReceivePopulationReport, ReceiveError)
    - Exposes all 8 ITerrariumHub methods (JoinEcosystem, LeaveEcosystem, TeleportCreature, AnnouncePeer, RequestWorldState, Heartbeat, RequestPeerList, ReportPopulation)
    - Auto-reconnect with exponential backoff matching Heisenberg's spec: immediate → 2s → 10s → 30s → 60s → give up
    - Connection state management (Connecting, Connected, Disconnected, Reconnecting) with OnStateChanged event
    - IAsyncDisposable for proper cleanup
    - Uses nameof(ITerrariumHub.*) and nameof(ITerrariumClient.*) for type-safe method references
  - `Components/ConnectionStatus.razor` — Blazor component showing live connection state
    - LED indicator using Glass theme tokens (glass-led--active/idle/waiting)
    - Subscribes to TerrariumHubClient.OnStateChanged for real-time updates
    - Integrated into MainLayout.razor status bar (replaces static LED)
  - Updated `Program.cs` — registered TerrariumHubClient as singleton in DI
  - Updated `Terrarium.Web.csproj` — added Microsoft.AspNetCore.SignalR.Client package + Terrarium.Net project reference
- **Build:** `dotnet build src/Terrarium.Web/Terrarium.Web.csproj` — all green
- **Design decisions:**
  - Singleton lifetime for TerrariumHubClient — one connection per Blazor circuit, shared across components
  - Events (Func<T, Task>) over interfaces for callback subscriptions — simpler component integration
  - Hub URL constructed from Aspire service discovery config (Services:server:https:0 or Services:server:http:0)
