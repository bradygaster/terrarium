# Decision: CORS Configuration for Terrarium Server

**By:** Gus
**Date:** 2025-07-17
**Status:** Implemented

## What
Added a default CORS policy to `Terrarium.Server/Program.cs` that allows:
- Origins: `localhost` (any port), `127.0.0.1`, and `*.internal` (Aspire service discovery)
- Methods: GET, POST (required by SignalR)
- Headers: all
- Credentials: true (required by SignalR negotiate)

## Why
- SignalR requires CORS when clients connect cross-origin (browser-direct scenarios)
- Current Blazor Server hub client runs server-side so CORS isn't blocking today, but any future browser-direct SignalR or external tooling would fail without it
- `SetIsOriginAllowed` with host check is flexible enough for dev without being wide-open in production
- `AllowCredentials()` is mandatory for SignalR's cookie/token-based negotiate handshake

## Hub Path Confirmed
- Server maps `TerrariumHub` at `/hubs/terrarium` — canonical path, no change needed
- Jesse is fixing the client side to connect to `/hubs/terrarium` (was `/terrarium`)
