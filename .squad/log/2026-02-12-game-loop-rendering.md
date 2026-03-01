# Session: 2026-02-12 — Game Loop and Client Rendering

**Requested by:** Brady (bradygaster)  
**Date:** 2026-02-12

## Summary

Three key components integrated to make the game loop visible to players:
1. **Mike** built `EcosystemSimulationWorker` — server-side game loop BackgroundService
2. **Skyler** wired client-side rendering from SignalR world state updates
3. **Hank** replaced diagnostic tests with 8 real Playwright integration tests

All three projects build clean. Pushed to branch: **terrarium-10**

## What Was Done

### Mike: EcosystemSimulationWorker (Server Game Loop)
- Created `src/Terrarium.Server/Workers/EcosystemSimulationWorker.cs` — a `BackgroundService` that:
  - Seeds ecosystem with 2000–3000 plants, 200–300 herbivores, 30–50 carnivores
  - Runs 500ms simulation tick loop
  - Broadcasts `EcosystemTick` (tick #, organism count, peer count) and `WorldStateUpdate` (full creature list with positions) to all clients
  - Population capped at 5000 organisms
  - Uses `IHubContext<TerrariumHub, ITerrariumClient>` for SignalR broadcasting
- Added `CreatureStateData` class and `Creatures` list to `WorldStateUpdate` in `src/Terrarium.Net/Messages/WorldStateUpdate.cs`
- Registered worker in `src/Terrarium.Server/Program.cs` via `AddHostedService<EcosystemSimulationWorker>()`
- **Key decision:** Simple in-process simulation for now — no Orleans grains yet (this is demo scaffolding)

### Skyler: Client-Side Rendering Integration
- Rewired `HandleWorldStateUpdate` in `Home.razor` to:
  - Map `CreatureStateData` from `WorldStateUpdate.Creatures` into `GameRenderState`/`CreatureRenderData`
  - Call `_gameView.RenderFrameAsync()` to draw creatures on canvas
  - Sync sidebar creature list (`_creatures`) from live server data
- Removed dummy hardcoded creatures from `OnInitialized` — all creature data now comes from server
- **Key finding:** Initial collision with Mike's `CreatureStateData` type — caught in build, resolved by removing duplicate definition
- Data flow established: Server simulation → SignalR `ReceiveWorldStateUpdate` → Client mapping → Canvas rendering

### Hank: Integration Tests (Playwright)
- Replaced 9 diagnostic-only tests in `terrarium-diagnostics.spec.js` with 8 real assertions in `terrarium-integration.spec.js`
- Tests validate:
  1. Map renders with non-zero canvas dimensions and drawn pixels
  2. Connection status LED shows "Connected" (`glass-led--active` class)
  3. Organisms appear on canvas (non-terrain pixels OR population > 0)
  4. Tick counter advances over 5 seconds
  5. Population stats show organisms > 0 in statusbar and sidebar
  6. Ecosystem status shows "Running" label with active LED
  7. Canvas is interactive (mouse drag changes viewport pixels)
  8. Event log shows activity (≥1 message entry)
- **Key decision:** From diagnostics (pure logging) to integration assertions (catch regressions)
- Run via: `cd src/Terrarium.Web.Tests.E2E && npx playwright test` (requires app running at `http://localhost:5190`)

## Outcomes

✅ All three projects build clean with zero errors  
✅ Game loop broadcasts state every 500ms to all clients  
✅ Client canvas renders incoming creatures in real-time  
✅ Integration tests pass and validate end-to-end behavior  
✅ Code pushed to branch: **terrarium-10**

## Key Insights

- **The missing link was the render call:** SignalR was working but nobody was calling `GameView.RenderFrameAsync()` in response to world state updates. Skyler fixed this.
- **Two-type mapping is intentional:** `CreatureStateData` (wire format) stays in Terrarium.Net; `CreatureRenderData` (rendering format) stays in Terrarium.Web. Decoupling prevents server concerns from leaking into UI.
- **Demo simulation is sufficient for player experience:** The simple in-memory EcosystemSimulationWorker makes the UI visually alive while the real GameEngine integration happens in later sprints.
- **Integration testing changes game development:** Playwright assertions caught what unit tests miss — actual rendering, network state, and user-visible feedback.

## Files Changed

**Mike:**
- `src/Terrarium.Server/Workers/EcosystemSimulationWorker.cs` (new)
- `src/Terrarium.Net/Messages/WorldStateUpdate.cs` (added CreatureStateData)
- `src/Terrarium.Server/Program.cs` (registered worker)

**Skyler:**
- `src/Terrarium.Web/Components/Pages/Home.razor` (HandleWorldStateUpdate rewritten)
- `src/Terrarium.Net/Messages/WorldStateUpdate.cs` (removed duplicate CreatureState)

**Hank:**
- `src/Terrarium.Web.Tests.E2E/tests/terrarium-integration.spec.js` (8 new tests)
- `src/Terrarium.Web.Tests.E2E/tests/terrarium-diagnostics.spec.js` (removed)

## Next Steps

- Orleans grain integration for persistent world state (Sprint 12)
- Real GameEngine wiring to replace demo simulation
- Multi-ecosystem support (currently broadcasts to all clients globally)
