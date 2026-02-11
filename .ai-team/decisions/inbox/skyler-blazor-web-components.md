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
