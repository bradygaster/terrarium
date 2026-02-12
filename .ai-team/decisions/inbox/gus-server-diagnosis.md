# Server Connectivity Diagnosis

**Author:** Gus (Server Dev)  
**Date:** 2025-07-17  
**Status:** Diagnostic — no code changes made

## Summary

Three issues found that explain why the web frontend shows "network status: red" and no traffic reaches the server.

---

## Finding 1: Hub Path Mismatch (CRITICAL)

**Server maps:**  
`app.MapHub<TerrariumHub>("/hubs/terrarium");` → `Program.cs:66`

**Client connects to:**  
`.WithUrl($"{serverUrl}/terrarium")` → `TerrariumHubClient.cs:52`

The server exposes the hub at `/hubs/terrarium` but the client tries to connect to `/terrarium`. The SignalR negotiate request will 404.

**Fix:** Either change the server to `app.MapHub<TerrariumHub>("/terrarium")` or change the client to `.WithUrl($"{serverUrl}/hubs/terrarium")`. Recommend aligning to `/hubs/terrarium` (ASP.NET convention).

---

## Finding 2: No CORS Configuration (CRITICAL)

**Server `Program.cs`:** Zero CORS setup. No `AddCors()`, no `UseCors()`, no CORS policy defined anywhere.

When running under Aspire with separate ports (server on 5180, web on 5190), the web app's SignalR client makes cross-origin requests. Without CORS:
- The SignalR negotiate POST will be blocked by the browser's preflight check
- WebSocket upgrade requests may also fail

**Note:** This applies to browser-initiated connections. Since Terrarium.Web is Blazor Server (server-side rendering), the `TerrariumHubClient` runs on the server process, not the browser. In that case, CORS is not required — service-to-service calls bypass the browser entirely. However, if any JavaScript-based SignalR connections are added later, CORS will be needed.

**Verdict:** CORS is not the blocking issue for Blazor Server, but should be added for future-proofing.

---

## Finding 3: Hub Connection Never Started (CRITICAL)

**`TerrariumHubClient`** is registered as a singleton in DI (`Program.cs:31`), and the `Home.razor` page wires up all callbacks in `OnInitialized()`. But **no component or hosted service ever calls `HubClient.StartAsync()`**.

The connection is built but never opened. This is the primary reason there's zero traffic.

**Fix:** Either:
- Call `await HubClient.StartAsync()` in `Home.razor`'s `OnAfterRenderAsync(firstRender)`, or
- Create a `BackgroundService` / `IHostedService` that starts the connection at app startup

---

## Aspire Service Discovery: ✅ CORRECT

- `AppHost/Program.cs:10` registers server as `"server"`
- `Terrarium.Web/Program.cs:16` calls `AddTerrariumServices("server")` 
- `ServiceCollectionExtensions.cs:56` builds URI `https+http://server`
- `TerrariumHubClient.cs:47-49` resolves `Services:server:https:0` or `Services:server:http:0` from Aspire-injected config, falling back to `https+http://server`

The names match. Aspire service discovery is correctly wired.

---

## API Endpoints: ✅ CORRECT

Server exposes all expected endpoint groups:
- `/api/messaging` — welcome, MOTD, version
- `/api/discovery` — peer discovery
- `/api/species` — species management
- `/api/reporting` — population reporting
- `/api/charts` — chart data
- `/api/watson` — error reporting
- `/api/bugs` — bug reports
- `/api/usage` — usage analytics

The `Terrarium.Services` layer configures typed HttpClient instances for each, all pointed at `api/` base path. These match.

---

## Authentication: ✅ NO BLOCKER

No authentication middleware (`UseAuthentication`, `UseAuthorization`, `[Authorize]`) is present in `Program.cs` or on the hub. SignalR connections are unauthenticated — no auth barrier exists.

---

## Throttle Middleware: ⚠️ MINOR CONCERN

`ThrottleMiddleware` runs on all requests including WebSocket upgrades. At 60 req/min default, it could theoretically throttle SignalR negotiate + long-polling fallback requests during rapid reconnect cycles. Not a blocking issue but worth monitoring.

---

## Priority Fix Order

1. **Call `StartAsync()`** on `TerrariumHubClient` — without this, nothing connects
2. **Fix hub path** — server `/hubs/terrarium` vs client `/terrarium` mismatch
3. **Add CORS** — only needed if browser-direct SignalR connections are planned
