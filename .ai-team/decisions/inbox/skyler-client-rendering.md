### 2026-02-12: World state rendering pipeline wired end-to-end
**By:** Skyler
**What:** `HandleWorldStateUpdate` in `Home.razor` now maps `CreatureStateData` from `WorldStateUpdate` into `GameRenderState`/`CreatureRenderData` and calls `GameView.RenderFrameAsync()`. Dummy creatures removed from `OnInitialized` — all creature data comes from the server via SignalR.
**Why:**
- **This was the missing link:** The SignalR client received world state updates but just logged them — no render call, no creature mapping. The canvas showed terrain but zero creatures.
- **Two-type mapping is intentional:** `CreatureStateData` (Terrarium.Net) is the wire format from the server; `CreatureRenderData` (Terrarium.Web.Rendering) is the canvas rendering format. They're similar but decoupled — rendering may need fields (SrcX, SrcY, FrameSize) that the server doesn't care about.
- **Sidebar synced from world state:** `_creatures` list (drives `CreaturePanel`) is now populated from `WorldStateUpdate.Creatures` with live positions, so the sidebar reflects real server data.
- **Converged with Mike on `CreatureStateData`:** Mike added his creature DTO to `WorldStateUpdate` in parallel. I initially added a duplicate — caught it in build, removed mine, and mapped to his type. No conflict remains.
- **48px default FrameSize:** Matches the sprite sheet frame size used by `terrarium-renderer.js`. Creatures without sprites render as colored circles (fallback in JS renderer).
