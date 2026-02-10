# Work Routing

How to decide who handles what.

## Routing Table

| Work Type | Route To | Examples |
|-----------|----------|----------|
| Architecture & solution structure | Heisenberg | .NET version targeting, project dependencies, modernization strategy, solution-wide decisions |
| WPF client & UI | Jesse | TerrariumClient, Controls, Glass, Renderer, user-facing components |
| DirectX & graphics | Jesse | DXVBLib, Graphics, rendering pipeline, visual effects |
| Server & web services | Gus | TerrariumServer MVC, web APIs, data persistence, server-side logic |
| P2P networking | Mike | HttpListener, peer discovery, creature teleportation, network protocol |
| Game engine & organism logic | Mike | Game, OrganismBase, creature lifecycle, simulation engine, Services |
| Assembly security & validation | Mike | AsmCheck, Configuration, code access security, creature DLL loading |
| Code review | Heisenberg | Review PRs, check quality, architectural consistency |
| Testing & QA | Hank | Unit tests, integration tests, TerrariumServer.Tests, build verification |
| Scope & priorities | Heisenberg | What to build next, trade-offs, decisions |
| Session logging | Scribe | Automatic — never needs routing |

## Rules

1. **Eager by default** — spawn all agents who could usefully start work, including anticipatory downstream work.
2. **Scribe always runs** after substantial work, always as `mode: "background"`. Never blocks.
3. **Quick facts → coordinator answers directly.** Don't spawn an agent for "what port does the server run on?"
4. **When two agents could handle it**, pick the one whose domain is the primary concern.
5. **"Team, ..." → fan-out.** Spawn all relevant agents in parallel as `mode: "background"`.
6. **Anticipate downstream work.** If a feature is being built, spawn the tester to write test cases from requirements simultaneously.
