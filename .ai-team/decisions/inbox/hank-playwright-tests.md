# Decision: Playwright Integration Tests Replace Diagnostics

**By:** Hank (Tester/QA)
**Date:** 2025-07-XX
**Status:** Implemented

## What

Replaced `terrarium-diagnostics.spec.js` (9 diagnostic-only tests) with `terrarium-integration.spec.js` (8 integration tests with real assertions).

## Why

The diagnostic suite was pure logging — `console.log` everywhere, zero assertions on actual behavior. Brady said "it looks like it works but it doesn't." Diagnostics don't catch regressions. Real assertions do.

## Test inventory

| # | Test name | What it validates |
|---|-----------|-------------------|
| 1 | map renders | Canvas visible, non-zero dimensions, has drawn pixels |
| 2 | connection status green | LED class `glass-led--active`, label "Connected" |
| 3 | organisms appear on canvas | Non-terrain pixels OR population > 0 |
| 4 | tick counter advances | Tick count increases over 5 seconds |
| 5 | population stats show organisms | Population > 0 in statusbar and sidebar |
| 6 | ecosystem status shows running | "Running" label + active LED |
| 7 | canvas is interactive | Mouse drag changes viewport pixel content |
| 8 | event log shows activity | ≥1 message-log entry with text |

## Running

```bash
cd src/Terrarium.Web.Tests.E2E
npx playwright test          # run all
npx playwright test --list   # list without running
```

Requires the app to be running at `http://localhost:5190` (start with Aspire first).

## Impact on other agents

- **Skyler/Jesse:** If you rename CSS classes (`.game-view__canvas`, `.glass-led--active`, `.connection-status__label`, `.ecosystem-status__metric`, `.glass-statusbar__section`, `.message-log__entry`), update the test selectors.
- **Mike:** Tests expect tick count to advance and population > 0 once SignalR data flows. If the game loop contract changes, tests may need updating.
