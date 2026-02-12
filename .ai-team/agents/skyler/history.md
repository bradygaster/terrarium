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

### 2026-02-11: Sprint 8 — Web Game Renderer (#55, #57, #58, #59)
- **What I built:**
  - `wwwroot/js/terrarium-renderer.js` — Full Canvas 2D rendering engine (ES module)
    - Terrain tile rendering using `background.bmp` and `dirt.bmp` assets, with grid overlay
    - Viewport system with pan (mouse drag, WASD/arrows) and zoom (scroll wheel, +/- keys)
    - Zoom-toward-cursor for natural zoom UX, clamped 0.25x–4.0x
    - Sprite rendering at world coordinates with viewport transform
    - Text overlay API: creature labels (with selection highlight), status overlays with rounded-rect background
    - Tooltip on creature hover showing name/species/energy
    - Click-to-select creature with dashed selection ring, Escape to deselect
    - Keyboard shortcuts: WASD/arrows pan, +/- zoom, 0 reset viewport, Escape deselect
    - `renderFrame()` orchestrates full frame: clear → terrain → creatures → labels → status → tooltip
    - JS→.NET callbacks via `DotNetObjectReference` for creature selection events
  - `Rendering/IGameRenderer.cs` — C# interface defining the renderer contract
    - Methods: Initialize, Clear, DrawTerrain, DrawSprite, DrawText, DrawCreatureLabel, DrawStatusOverlay, RenderFrame, Resize, SetViewport, PanViewport, SetZoom, GetViewport
    - DTOs: `ViewportState`, `GameRenderState`, `CreatureRenderData`
  - `Rendering/CanvasGameRenderer.cs` — JS interop implementation of `IGameRenderer`
    - Imports `terrarium-renderer.js` as ES module
    - Proxies all calls to JS, manages `DotNetObjectReference` lifecycle
    - Events: `OnCreatureSelected`, `OnCreatureDeselected` (wired from JS `[JSInvokable]`)
  - `Components/GameView.razor` — Blazor component hosting the canvas
    - Creates/initializes `CanvasGameRenderer` on first render
    - Exposes `RenderFrameAsync()` and `Renderer` property to parent components
    - Selection bar UI showing selected creature name
    - Parameters: `WorldWidth`, `WorldHeight`, `OnCreatureClicked`, `OnSelectionCleared`
  - CSS: `.game-view` styles in `app.css` using Glass theme tokens
  - Updated `_Imports.razor` with `Terrarium.Web.Rendering` namespace
- **Build:** `dotnet build src/Terrarium.Web/Terrarium.Web.csproj` — 0 errors, 0 warnings
- **Design decisions:**
  - Renderer lives in `Terrarium.Web.Rendering/` (no cross-project dependency on `Terrarium.Game`)
  - ES module pattern for JS (not global scripts) — matches existing `terrarium-interop.js` pattern
  - `CanvasGameRenderer` is per-component, not singleton — each `GameView` owns its renderer lifecycle
  - Existing `TerrariumViewport.razor` preserved (not replaced) — `GameView` is the new full-featured component
  - Terrain uses pre-loaded `Image` elements (not `ImageBitmap`) for BMP compatibility
  - Hit-testing uses simple bounding-box on creature array (sufficient for current creature counts)

## Learnings

📌 `terrarium-renderer.js` is the canonical rendering engine — all Canvas operations go through it, not `terrarium-interop.js` (which remains as legacy stub)
📌 `IGameRenderer` interface in `Terrarium.Web.Rendering` is the C# contract for rendering — implementations must be `IAsyncDisposable`
📌 JS↔Blazor callbacks use `DotNetObjectReference` + `[JSInvokable]` pattern — method names must match exactly between JS `invokeMethodAsync('MethodName')` and C# `[JSInvokable] public Task MethodName()`
📌 Terrain assets are BMP files in `wwwroot/assets/terrain/` — `background.bmp` (default ground) and `dirt.bmp` (variant tile)
