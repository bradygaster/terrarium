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
