### 2026-02-11: Error handling architecture for Sprint 12

**By:** Heisenberg

**What:** Implemented comprehensive error handling and resilience across the entire Terrarium stack:
1. TerrariumErrorBoundary component wrapping all pages — catches render exceptions with user-friendly recovery UI
2. Graceful degradation in Home.razor — automatic switch to local-only mode when server unreachable (after 3 connection attempts)
3. SignalR exponential backoff reconnection — confirmed already implemented in TerrariumHubClient (immediate, 2s, 10s, 30s, 60s)
4. HTTP retry logic — added StandardResilienceHandler to all service clients (3 retries, exponential backoff, circuit breaker, timeout)
5. Enhanced error categorization — distinguish network errors, timeouts, and validation failures

**Why:** 
- **Resilience:** Game must continue running even when server is down — local-only mode preserves user experience
- **User trust:** ErrorBoundary prevents white screens — users see friendly error messages and can recover
- **Network reliability:** StandardResilienceHandler handles transient failures transparently (cloud services, mobile networks)
- **Debugging:** Environment-aware error display (stack traces in dev, generic messages in prod) + structured logging
- **Maintainability:** Centralized error handling patterns reduce duplication across pages

**Rationale:**
- ErrorBoundary is a standard Blazor pattern — custom TerrariumErrorBoundary adds telemetry hooks and fallback navigation
- Local-only mode aligns with original Terrarium's P2P architecture — game should degrade gracefully, not crash
- StandardResilienceHandler is .NET's built-in solution — no need for custom Polly policies
- Three connection attempts balances user patience (don't give up too soon) vs. responsiveness (don't hang forever)
- Exponential backoff prevents thundering herd on server restart

**Impact:**
- Users experience fewer crashes and hangs
- Server downtime no longer blocks gameplay
- Transient network errors auto-retry without user intervention
- Error logs become more actionable with categorization
- Future enhancements (toast notifications, telemetry, offline queue) have a foundation

**References:**
- docs/error-handling-architecture.md
- src/Terrarium.Web/Components/Shared/TerrariumErrorBoundary.razor
- src/Terrarium.Web/Components/Pages/Home.razor
