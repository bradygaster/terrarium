# Decision: GameView self-renders initial terrain frame

**Date:** 2025-07-17
**Author:** Jesse (Client Dev)
**Status:** Implemented

## Context

After SignalR connectivity was fixed, the canvas remained blank blue. The previous fix added `RenderFrameAsync(new GameRenderState())` in `Home.razor`'s `OnAfterRenderAsync`, but this call was silently a no-op due to a Blazor lifecycle race condition.

## Problem

Blazor `OnAfterRenderAsync` fires **parent-first**: `Home.razor` executes before `GameView.razor`. When Home called `_gameView.RenderFrameAsync()`, GameView's renderer hadn't been created yet (`_renderer` was null, `IsInitialized` was false). The guard returned silently. After GameView's own `OnAfterRenderAsync` completed initialization, nobody called `renderFrame()` again.

## Decision

**GameView owns its initial render.** Immediately after `InitializeAsync()` completes in `GameView.OnAfterRenderAsync`, the component calls `RenderFrameAsync(new GameRenderState())` to draw green terrain tiles. Home.razor no longer attempts to render — it only handles SignalR.

## Rationale

- Components should be self-contained: a game view should display something meaningful the moment it initializes
- Eliminates parent-child timing dependencies
- Simpler mental model: GameView = init + first render; Home = network + game state

## Impact

- Canvas shows green grass terrain grid immediately on page load
- No dependency on server data for initial visual feedback
- Logging added throughout the render chain for observability
