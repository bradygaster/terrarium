# Decision: Fix Client Hub URL and Add Connection Startup

- **Date:** 2025-07-17
- **Author:** Jesse (Client Dev)
- **Status:** Implemented

## Context

The Blazor web client was never connecting to the SignalR hub due to three bugs:
1. Hub URL path was `/terrarium` but server maps at `/hubs/terrarium`
2. Same wrong URL in `GameServiceExtensions.cs`
3. `StartAsync()` was never called after wiring event handlers

## Decision

- Changed hub URL from `/terrarium` to `/hubs/terrarium` in both `TerrariumHubClient.cs` and `GameServiceExtensions.cs`
- Added `OnAfterRenderAsync(firstRender)` in `Home.razor` that:
  1. Renders an empty frame (terrain only) so users see green grass instead of blue CSS gradient
  2. Calls `HubClient.StartAsync()` with try/catch — falls back to local-only mode if server is unavailable
- Used `OnAfterRenderAsync` (not `OnInitializedAsync`) because the `GameView` canvas requires JS interop which is only available after first render in Blazor Server

## Coordination

Gus is fixing the server-side hub mapping separately. Both fixes must land for end-to-end connectivity.
