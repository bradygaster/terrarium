# Decision: Server Logging & Missing Game Loop Diagnosis

**Date:** 2025-07-17
**Author:** Gus (Server Dev)
**Status:** Decided

## Context

Brady reported the frontend shows "Connected" but the canvas is blank with zero logs from any component.

## Findings

1. **TerrariumHub was missing `OnConnectedAsync`** — client connections were never logged. Only disconnections were tracked via `OnDisconnectedAsync`.

2. **No game loop or world state broadcaster exists on the server.** The `ITerrariumClient` interface defines `ReceiveEcosystemTick` and `ReceiveWorldStateUpdate` callbacks, but **nothing on the server ever calls them**. The hub only relays data when clients call hub methods (like `ReportPopulation`). There is no `BackgroundService` that:
   - Runs a simulation tick
   - Broadcasts `EcosystemTick` to connected clients
   - Pushes `WorldStateUpdate` periodically

3. **`RequestWorldState` returns empty data** — `WorldWidth: 0, WorldHeight: 0, TickNumber: 0, OrganismCount: {peerCount}`. The TODO says "Sprint 11 — fetch from EcosystemGrain" but no grain or in-memory substitute exists.

4. **No startup banner** was being logged.

## What Was Done

- Added `OnConnectedAsync` to `TerrariumHub` with connection logging
- Added startup banner log in `Program.cs`
- Added heartbeat log to `NonPageServicesWorker` loop cycle

## What Still Needs to Happen (Root Cause of Blank Canvas)

The server needs a **game loop service** — a `BackgroundService` that:
1. Maintains world state (terrain grid, organism positions)
2. Runs periodic ticks (e.g., every 1 second)
3. Broadcasts `EcosystemTick` and `WorldStateUpdate` to all clients via `IHubContext<TerrariumHub, ITerrariumClient>`

Without this, the server is a passive relay — it only sends data when clients push data first. Since there's no game simulation, no client has anything to push.

The `IEcosystemNotifier` interface exists in `Terrarium.Net` and defines exactly the right methods (`NotifyTickAsync`, `NotifyWorldStateAsync`), but **no class implements it** on the server side.

## Decision

- Logging changes are immediate (this PR)
- Game loop service is a separate task — requires design decisions about world dimensions, tick rate, terrain generation, and organism spawning
