### 2026-02-11: SignalR Client Service Architecture in Terrarium.Web
**By:** Skyler (Frontend Web Dev)
**Status:** Implemented
**Sprint:** 7
**Issue:** #51

**What:**
1. `TerrariumHubClient` is registered as a **singleton** in DI. One SignalR connection per Blazor Interactive Server circuit.
2. Hub URL is resolved via Aspire service discovery config keys (`Services:server:https:0` or `Services:server:http:0`), falling back to `https+http://server`. The hub endpoint is `/terrarium`.
3. All ITerrariumClient callbacks are exposed as `Func<T, Task>` events — components subscribe/unsubscribe in OnInitialized/Dispose.
4. All ITerrariumHub methods are wrapped as async methods with `Async` suffix (e.g., `JoinEcosystemAsync`).
5. Auto-reconnect uses Heisenberg's 5-attempt backoff schedule: 0s, 2s, 10s, 30s, 60s.
6. `ConnectionStatus.razor` component integrated into MainLayout status bar. Uses Glass LED tokens for visual state.
7. After reconnect, clients must re-call `JoinEcosystemAsync` and `AnnouncePeerAsync` per Heisenberg's architecture doc (new connection ID on reconnect).

**Why:** Sprint 7 deliverable — the Blazor web client needs a SignalR connection to TerrariumHub to participate in real-time ecosystem events, teleportation, and peer management. This replaces all legacy TCP/HTTP P2P networking with a single WebSocket spoke.

**Impact:** All components that need real-time data (EcosystemStatus, TerrariumViewport, CreaturePanel, MessageLog) can now inject `TerrariumHubClient` and subscribe to events. The Home page will need to be wired up to use this service (separate task).
