# Decision: SignalR Hub as Pure Contract Layer

**By:** Mike (Networking / Engine Dev)
**Date:** 2025-07-16
**Status:** Implemented (PR #113)
**Issue:** #16

## What

`Terrarium.Net` is a contract-only library. It defines interfaces (`ITerrariumHub`, `ITerrariumClient`), message types, and a thin `TerrariumHub` implementation. No business logic.

## Key Technical Decisions

1. **FrameworkReference, not NuGet** — `Microsoft.AspNetCore.App` FrameworkReference gives us SignalR for free on net10.0. No version pinning needed.

2. **Single-message teleport** — The legacy 4-step HTTP teleport protocol (version check → assembly check → assembly transfer → state transfer) is collapsed into a single `CreatureTeleport` message. The optional `AssemblyPayload` field carries base64-encoded assembly bytes only when the target peer doesn't have the species assembly. Assembly presence tracking will be handled by `PeerGrain` in Sprint 7.

3. **SignalR groups = ecosystems** — Each ecosystem ID maps to a SignalR group. `JoinEcosystem`/`LeaveEcosystem` manage group membership. This replaces the SOAP-based peer discovery polling loop.

4. **No Terrarium project dependencies** — `Terrarium.Net` depends only on the ASP.NET Core shared framework. Message types use `string` for state payloads (JSON) rather than referencing `Terrarium.OrganismBase` types. This keeps the contract boundary clean and allows the web client to reference it without pulling in game engine dependencies.

## Sprint 7 Integration Points

Every `// TODO: Sprint 7` comment in `TerrariumHub` marks where Orleans grain calls will be inserted:
- `JoinEcosystem` → `PeerGrain.RegisterPeer()`
- `LeaveEcosystem` → `PeerGrain.RevokeLease()`
- `TeleportCreature` → `PeerGrain` for routing, `EcosystemGrain` for validation
- `AnnouncePeer` → `PeerGrain` for lease management
- `RequestWorldState` → `EcosystemGrain.GetWorldState()`
- `OnDisconnectedAsync` → `PeerGrain.RevokeLease()`

## Why This Matters

The legacy networking stack (`HttpWebListener`, `NetworkEngine`, `PeerManager`) is ~3000 lines of custom TCP socket code, HTTP parsing, and binary serialization. The replacement is ~250 lines of typed interfaces and a thin hub. SignalR handles transport, reconnection, and message serialization. Orleans will handle state, leasing, and routing.
