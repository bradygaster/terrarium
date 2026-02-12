# Decision: Playwright E2E Diagnostic Setup

**By:** Hank (Tester/QA)
**Date:** 2026-02-12
**Requested by:** Brady

## What

Created `src/Terrarium.Web.Tests.E2E/` as a Node.js Playwright test project with 9 diagnostic tests targeting the Blazor web frontend and Terrarium server.

## Why

Brady reported: Web renders at localhost:5190 but shows "network status: red circle" and empty blue canvas with no traffic between frontend and server. We need browser-level diagnostic visibility.

## Architecture Decision: Node.js Playwright (not .NET Playwright)

Chose standalone Node.js over a .NET `Microsoft.Playwright` project because:
- Faster to set up (no csproj, no solution integration needed)
- Playwright's Node.js API has richer documentation and community support
- These are diagnostic/investigative tests, not regression tests in the .NET test matrix
- Can run independently with `npm test` — zero coupling to the .NET build

## Key Diagnostic Findings (from first run)

All 9 tests passed. Here's what they revealed:

1. **Connection Status: 🔴 Disconnected** — LED class is `glass-led--idle`, label says "Disconnected". Never transitions to Connected during 30s observation.

2. **SignalR Hub endpoint returns 404** — `POST http://localhost:5180/terrarium/negotiate?negotiateVersion=1` returns HTTP 404. The server does NOT have `MapHub<TerrariumHub>("/terrarium")` wired up. This is the root cause.

3. **Server is healthy** — `http://localhost:5180/` returns 200 "Terrarium Server", `/health` returns 200 "Healthy", `/alive` returns 200 "Healthy".

4. **Browser makes ZERO requests to :5180** — all traffic goes to :5190 (the Blazor Server host). This is correct for Blazor Server mode — the `TerrariumHubClient` runs server-side, not in the browser.

5. **Canvas exists but is blank** — 972×619px canvas with 0 non-transparent pixels. The renderer initializes but never receives world state data to draw.

6. **Blazor circuit is working** — WebSocket to `ws://localhost:5190/_blazor` is open and exchanging frames normally.

7. **Only console error: 404 for `/images/terrarium-icon.png`** — minor missing asset, not related to the connectivity issue.

8. **PeerList LED shows `glass-led--failed`** — indicates the peer list component detected connection failure.

## Root Cause Summary

**The server at localhost:5180 does not map the `/terrarium` SignalR hub endpoint.** The `TerrariumHubClient` in the Web project tries to connect to `{serverUrl}/terrarium` but gets a 404. The server's `Program.cs` likely needs `app.MapHub<TerrariumHub>("/terrarium")`.

## Files Created

- `src/Terrarium.Web.Tests.E2E/package.json` — Node.js project
- `src/Terrarium.Web.Tests.E2E/playwright.config.js` — Playwright config
- `src/Terrarium.Web.Tests.E2E/tests/terrarium-diagnostics.spec.js` — 9 diagnostic tests
- `src/Terrarium.Web.Tests.E2E/.gitignore` — ignores node_modules, test artifacts

## How to Run

```bash
cd src/Terrarium.Web.Tests.E2E
npm test              # headless
npm run test:headed   # with browser UI visible
```
