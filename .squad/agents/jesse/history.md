# Project Context

- **Owner:** bradygaster
- **Project:** .NET Terrarium 2.0 — peer-to-peer networked creature ecosystem game
- **Stack:** C#, .NET, WPF, ASP.NET MVC, DirectX, P2P networking
- **Created:** 2026-02-10

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2025-07-15 — Initial Domain Mapping (ClientWPF)

**Key finding: All `ClientWPF/` projects are empty scaffolds.** Every project contains only `Properties/AssemblyInfo.cs` (and for TerrariumClient, empty `App.xaml`/`MainWindow.xaml`). No business logic has been ported.

**The real implementation lives in `Client/`** — specifically:
- `Client/Renderer/` — Full DirectDraw rendering pipeline (~25 .cs files). The main class is `TerrariumDirectDrawGameView` (a `PictureBox` subclass).
- `Client/Glass/` — Custom WinForms theming framework (GlassPanel, GlassButton, GlassLabel, GlassStyleManager).
- `Client/Controls/` — WinForms custom controls (TitleBar, BottomPanel, DeveloperPanel, StatusBar, TickerBar, LEDs).
- `Client/ControlsWPF/` — **Partial WPF port** of controls. Has real XAML UserControls (GlassTitleBar, GlassBottomPanel, DeveloperPanel, TickerBar) with a shared ResourceDictionary for Glass styling. Includes 18 PNG button icons.
- `Client/TerrariumWPF/` — **Partial WPF port** of the main window. `MainForm.xaml` shows the target DockPanel layout; `MainForm.xaml.cs` creates `TerrariumDirectDrawGameView` and hosts it via `WindowsFormsHost`.
- `Client/Terrarium/` — Legacy WinForms main app with UI classes (MainForm, SplashScreen, TraceWindow, PropertySheet, etc.).

**Architecture observations:**
- Rendering pipeline: `DXVBLib` (COM interop) → `Renderer/DirectX/` (managed wrappers) → `TerrariumDirectDrawGameView` (game loop). The `Graphics` project and `IGraphicsEngine` interface are both empty — DX7 is used directly.
- WPF interop via `WindowsFormsHost` embedding the WinForms `PictureBox`-based game view. This causes WPF airspace issues.
- All projects target .NET Framework 4.0 (VS2010-era MSBuild). TerrariumClient is x86-only (required for DX7 COM interop).
- No project references exist in `ClientWPF/` yet — none of the skeleton projects reference each other.
- The Glass styling system (WinForms) maps to WPF `ResourceDictionary` styles. The legacy `ControlsWPF/ResourceDictionary.xaml` has `GlassGradient`, `GlassLabel`, `GlassButton`, and `GlassButtonTemplate` already defined.

**File inventory written to:** `ClientWPF/README.md`

📌 Team update (2026-02-10): MVC Server is a scaffold — all game logic lives in legacy ASMX — decided by Gus
📌 Team update (2026-02-10): Build must be green before new tests — decided by Hank
📌 Team update (2026-02-10): .NET 10 modernization sprint plan created — 14 sprints, WPF on .NET 10, Silk.NET OpenGL, gRPC P2P, Dapper+stored procs, process isolation, xUnit, System.Text.Json — decided by Heisenberg
📌 Team update (2026-02-11): Beth's voice is fearless, developer-first, community-driven — decided by bradygaster

📌 Team update (2026-02-11): Diagram standards — Mermaid only, no ASCII art. All diagrams must use Mermaid — decided by Badger, bradygaster
📌 Team update (2026-02-11): VB.NET respectful framing — never refer to VB.NET negatively, just "we're C# now" — decided by bradygaster
📌 Team update (2026-02-11): Orleans + SignalR hybrid recommended — 4 grain types for networking layer — decided by Heisenberg
📌 Team update (2026-02-11): CI pipeline created (.github/workflows/build.yml) targeting src/Terrarium.sln — decided by Hank
📌 Team update (2026-02-11): Services layer HttpClient-based, System.Text.Json, interface-first — decided by Mike
📌 Team update (2026-02-11): SignalR Hub contract locked (8 methods, 7 callbacks, error handling via ReceiveError callback) — decided by Mike
📌 Team update (2026-02-11): Terrarium.Web Blazor Interactive Server created (PR #118), SignalR-ready, Glass CSS integrated — decided by Skyler
📌 Team update (2026-02-11): Glass CSS expanded to 60+ tokens (~12 new component classes), all 76 original assets cataloged/extracted to wwwroot/assets/ — decided by Jesse
📌 Team update (2026-02-11): Server.Tests xUnit project (17 tests, 4 passing now) — decided by Hank
📌 Team update (2026-02-11): SDK Samples standalone structure (SimpleHerbivore/SimpleCarnivore/SimplePlant ported) — decided by Hank
📌 Team update (2026-02-11): Species & Reporting endpoints (assembly storage deferred, word filter deferred) — decided by Gus
📌 Team update (2026-02-11): Organism Isolation architecture (AssemblyLoadContext-based 3-layer sandbox) — decided by Heisenberg
📌 Team update (2026-02-11): Hub-and-spoke SignalR architecture finalized (rate limiting, heartbeat/lease, reconnect=rejoin) — decided by Heisenberg
📌 Team update (2026-02-11): Road ahead blog post (sprint-prep-the-road-ahead.md): 48 issues, 7 sprints, 89 minutes wall-clock — decided by Beth

### 2025-07-16 — Sprint 3: Glass Theming Expansion + Asset Catalog (PR #123)

**Glass CSS Expansion (#23):**
Expanded the Glass design system from base tokens+components to full UI coverage. Added ~60 new CSS custom properties for inputs, dropdowns, sidebar panels, toolbars, stat grids, and scrollbars. Added 12 new component classes: `.glass-sidebar` (frosted glass via `backdrop-filter`), `.glass-input`, `.glass-select`, `.glass-toolbar`, `.glass-stat-grid`, `.glass-minimap`, `.glass-dialog`, `.glass-ticker`, `.glass-tabs`, `.glass-icon-button`, plus themed scrollbars. All follow BEM naming and `--glass-*` token convention.

**Asset Catalog (#25):**
Searched entire legacy codebase and extracted ALL 76 original image assets to `src/Terrarium.Web/wwwroot/assets/`:
- `sprites/` — 19 files: creature sprite sheets (ant, beetle, inchworm, scorpion, spider at 24px+48px), plant variants (4 types at 24px+48px), teleporter
- `terrain/` — 2 files: background grass tile, dirt tile
- `ui/` — 32 files: WPF toolbar button icons (18), splash/screensaver, server website banners/borders (8), WinForms play buttons (2), watermark
- `cursors/` — 1 file: custom in-game cursor
- `icons/` — 5 files: tericon 16/32px, app icon copy, StyleEditor icon, ServerConfig icon
- `screenshots/` — 17 files: classic Whidbey screenshots, tutorials, documentation images, root whidbey_image001.jpg

Created `manifest.json` cataloging every asset with path, original location, and purpose.

**Key insight:** The `.svnbridge` subdirectories in `Client/ControlsWPF/Images/` contain duplicate copies of the button PNGs — excluded those to avoid redundancy. All unique assets are preserved.

### 2025-07-17 — Sprint 8: Web Sprite System (Issue #56)

**Created three-layer JavaScript sprite system** for HTML5 Canvas rendering:

- **`terrarium-sprites.js`** — Core classes: `SpriteSheet` (frame extraction from loaded `ImageBitmap`), `SpriteAnimation` (time-based or frame-indexed sequencing), direction/action mappings matching the 10×40 animal sheet layout. Factory helpers for animal, plant, and teleporter animations.
- **`sprite-manager.js`** — Singleton manager that loads sheets from `animations.json`, caches them, and provides `drawSprite()` (stateless) and `drawAnimated()` (time-based) entry points the renderer calls.
- **Updated `terrarium-interop.js`** — Blazor JS interop now exposes `loadSprites()`, `drawSprite()`, `drawFrame(worldState)`, `getSpriteStatus()`. `drawFrame` handles entity arrays with per-entity action/direction/frame data.
- **Updated `terrarium-renderer.js`** — Added `loadSprites()` and `drawCreatureSprite()` exports; `renderFrame()` now prefers `SpriteManager` over raw source coords.
- **Updated `TerrariumViewport.razor`** — Added `LoadSpritesAsync()` and `DrawSpriteAsync()` C# methods.
- **Updated `App.razor`** — Script loading order ensures globals are available before ES modules.

**Key insights:**
- The direction indices in `animations.json` are 1-based (E=1..NE=8), but row offsets within an action block are 0-based. Row = `actionBaseRow + (directionIndex - 1)`.
- The teleporter sheet is 768×48 (16 frames × 48px in a single row), not 10 columns like animals.
- `SpriteLoader` (existing) uses `createImageBitmap(blob)` from fetched BMPs — works well for the original `.bmp` format sprite sheets.
- `sprite-manager.js` and `terrarium-sprites.js` are loaded as globals (IIFE/const) so the ES module `terrarium-interop.js` can reference them. This matches the existing `sprite-loader.js` pattern.

### 2025-07-17 — Frontend Connectivity Diagnosis

**SignalR connection lifecycle:**
- `TerrariumHubClient` (`src/Terrarium.Web/Services/TerrariumHubClient.cs`) is a singleton managing SignalR connection to the game server hub. Reads server URL from Aspire service discovery config keys `Services:server:https:0` / `Services:server:http:0`.
- `ConnectionStatus.razor` shows LED indicator based on `HubClient.State` and `OnStateChanged` event — purely reactive, does not initiate connections.
- `Home.razor` wires up all hub callback events in `OnInitialized()` but does NOT call `HubClient.StartAsync()` — the connection is never opened.

**Hub URL mismatch:**
- Server maps hub at `/hubs/terrarium` (`Terrarium.Server/Program.cs:66`)
- Client connects to `{serverUrl}/terrarium` (missing `/hubs/` prefix) in `TerrariumHubClient.cs:52`
- `GameServiceExtensions.cs:53` also uses wrong path `"https+http://server/terrarium"`

**Canvas rendering pipeline:**
- `GameView.razor` → `CanvasGameRenderer` → `terrarium-renderer.js` (ES module)
- On first render: canvas is initialized, terrain tiles loaded from `/assets/terrain/*.bmp`, viewport set to 5000×5000, events bound
- `renderFrame(worldState)` draws: clear → terrain → creatures → overlays → tooltip → performance stats
- `drawTerrain(null)` works fine standalone — tiles green grass from loaded BMPs or falls back to `#2d5a27` fill
- Canvas appears "blue" when empty because `clearRect` makes it transparent, showing parent's Glass theme gradient (`--glass-gradient-panel-bottom: #000060`)
- No game loop or initial `renderFrame()` call exists — canvas stays cleared after init

**Aspire wiring:**
- `AppHost/Program.cs`: web `.WithReference(server)` correctly injects service discovery URLs
- No CORS needed — Blazor Server's SignalR client runs server-side (backend HTTP), not in browser

### 2025-07-17 — Hub URL + Connection Startup Fix

**Fixed three client-side connectivity bugs preventing SignalR from ever connecting:**

1. **Hub URL path mismatch** — `TerrariumHubClient.cs` was connecting to `{serverUrl}/terrarium` but the server maps the hub at `/hubs/terrarium`. Changed to `{serverUrl}/hubs/terrarium`.
2. **Same URL bug in GameServiceExtensions.cs** — `NetworkEngineOptions.HubUrl` was `"https+http://server/terrarium"`, changed to `"https+http://server/hubs/terrarium"`.
3. **`StartAsync()` never called** — `Home.razor` wired all event handlers in `OnInitialized()` but never opened the connection. Added `OnAfterRenderAsync(firstRender)` that calls `await HubClient.StartAsync()` with try/catch fallback to local-only mode.
4. **Initial terrain render** — Added `RenderFrameAsync(new GameRenderState())` call in `OnAfterRenderAsync` before starting SignalR, so users see green terrain tiles immediately instead of a blue (CSS gradient) canvas.

**Key files modified:**
- `src/Terrarium.Web/Services/TerrariumHubClient.cs` (line 52: URL fix)
- `src/Terrarium.Game/GameServiceExtensions.cs` (line 53: URL fix)
- `src/Terrarium.Web/Components/Pages/Home.razor` (added `OnAfterRenderAsync`)

### 2025-07-17 — Render Pipeline Fix + Observability Logging

**Root cause of blank blue canvas:** Blazor `OnAfterRenderAsync` fires parent-first. `Home.razor`'s handler called `_gameView.RenderFrameAsync()` before `GameView.razor`'s handler had initialized the `CanvasGameRenderer`. The guard `_renderer is { IsInitialized: true }` silently returned, so `renderFrame()` in JS was never called. After `GameView` finished its own init, no one re-triggered a render — canvas stayed transparent (showing blue CSS gradient).

**Fix:**
1. **Moved initial `renderFrame` into `GameView.OnAfterRenderAsync`** — immediately after `InitializeAsync()` completes, the component calls `_renderer.RenderFrameAsync(new GameRenderState())`. This guarantees terrain draws the moment the renderer is ready, regardless of parent timing.
2. **Removed premature render call from `Home.razor`** — the old `RenderFrameAsync()` call in Home's `OnAfterRenderAsync` was a no-op due to the race condition. Removed it with a comment explaining why.

**Observability logging added (Brady directive: "any entity that is alive should log"):**
- `GameView.razor` — `ILogger<GameView>` injection, logs at init start, init complete, and first terrain frame.
- `Home.razor` — Added log at `OnInitialized` and `OnAfterRenderAsync` startup (already had `ILogger<Home>`).
- `terrarium-renderer.js` — `console.log` at: `initialize()` entry/exit with canvas/world dimensions, terrain tile load success/failure with image dimensions, `renderFrame()` first frame with tile status and creature count, `drawTerrain()` skip warnings.
- `TerrariumHubClient.cs` — Already had comprehensive ILogger usage (connection lifecycle, all callbacks, reconnect events). No changes needed.
- `GameRenderBridge.cs`, `GameServiceBridge.cs` — Already had ILogger with proper logging. No changes needed.

**Key insight:** In Blazor Interactive Server, `OnAfterRenderAsync` fires top-down (parent before child). Any parent that needs to interact with a child component's JS interop must either: (a) let the child self-initialize and self-render, or (b) await a signal from the child that init is complete. Option (a) is simpler and what we chose.

**Key files modified:**
- `src/Terrarium.Web/Components/GameView.razor` (ILogger, initial render after init)
- `src/Terrarium.Web/Components/Pages/Home.razor` (removed premature render, added init logging)
- `src/Terrarium.Web/wwwroot/js/terrarium-renderer.js` (console.log at init, terrain load, renderFrame, drawTerrain)
