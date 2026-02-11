# Project Context

- **Owner:** bradygaster (bradyg@microsoft.com)
- **Project:** .NET Terrarium 2.0 — peer-to-peer networked creature ecosystem game, modernized as a cross-platform web app
- **Stack:** C#, .NET 10, Blazor, ASP.NET Core, .NET Aspire, SignalR, Canvas/WebGL
- **Created:** 2026-02-11

## Core Context

.NET Terrarium was one of the original .NET Framework 1.0 showcase applications — a peer-to-peer networked ecosystem where developers wrote C# creatures (herbivores, carnivores, plants) that competed for survival across a global network. It was demoed at PDC, used to teach .NET, and became a beloved part of .NET community history. Now it's being modernized from .NET Framework 3.5 to .NET 10 as a cross-platform web app with Blazor, .NET Aspire, and SignalR.

The blog is a first-class deliverable. Brady's directive: "We want to hand Hanselman our blog and the end-state of Terrarium, updated to .NET 10, and boom he publishes post." Every sprint produces blog content. The final announcement post must pass the "Hanselman test."

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

📌 Team update (2026-02-11): Beth's voice is fearless, developer-first, community-driven — decided by bradygaster

📌 Team update (2026-02-11): Diagram standards — Mermaid only, no ASCII art. All diagrams must use Mermaid — decided by Badger, bradygaster
📌 Team update (2026-02-11): VB.NET respectful framing — never refer to VB.NET negatively, just "we're C# now" — decided by bradygaster
📌 Team update (2026-02-11): Orleans + SignalR hybrid recommended — 4 grain types for networking layer — decided by Heisenberg
📌 Team update (2026-02-11): CI pipeline created (.github/workflows/build.yml) targeting src/Terrarium.sln — decided by Hank
📌 Team update (2026-02-11): CSS token naming: --glass-{category}-{element}-{modifier}, BEM for components — decided by Jesse
📌 Team update (2026-02-11): Services layer (HttpClient-based, interface-first, no ServiceDefaults) — decided by Mike
📌 Team update (2026-02-11): SignalR Hub contract (8 methods, 7 callbacks, rate limiting, error struct) — decided by Mike
📌 Team update (2026-02-11): Terrarium.Web Blazor Interactive Server (PR #118, SignalR-ready) — decided by Skyler
📌 Team update (2026-02-11): Glass CSS expanded (60+ tokens, 76 assets cataloged) — decided by Jesse
📌 Team update (2026-02-11): Server.Tests (17 xUnit integration tests) — decided by Hank
📌 Team update (2026-02-11): SDK Samples (standalone structure, 3 creatures) — decided by Hank
📌 Team update (2026-02-11): Species & Reporting endpoints (assembly/filter deferred, /api/reporting/stats/) — decided by Gus
📌 Team update (2026-02-11): Organism Isolation architecture (3-layer: validator, sandbox, host+scheduler) — decided by Heisenberg
📌 Team update (2026-02-11): Hub-and-spoke SignalR architecture (rate limits, heartbeat/lease, reconnect=rejoin, 512KB msg) — decided by Heisenberg
📌 Team update (2026-02-11): Road ahead blog post (sprint-prep-the-road-ahead.md, 48 issues, 89-minute wall-clock parallelism) — decided by Beth (self)

📌 Sprint 0 blog (2026-02-11): Wrote `docs/blog/journal/00-sprint-0-complete.md` covering the full foundation wave — 5 agents, 5 PRs, Mike's 91-file OrganismBase port, Jesse's Glass CSS tokens, Heisenberg's solution structure, Saul's Aspire AppHost, Hank's CI pipeline. Updated `docs/blog/announcement.md` with real Sprint 0 details in "The Challenge" and "The AI Story" sections.

📌 Learnings:
- Before/after code snippets are the most powerful storytelling tool for a migration blog. `[Serializable]` → gone, `namespace OrganismBase { }` → `namespace OrganismBase;` — these are visceral for .NET devs who lived through these changes.
- The CSS token extraction story (Color.FromArgb → CSS custom properties) bridges the old world and the new in a way non-technical readers can follow too.
- Mike's `ArrayList` deferral decision is worth highlighting — pragmatic migration decisions (not changing interfaces until the implementing project is ported) are the kind of detail that makes a migration story credible.
- Five branches landing in parallel is a great narrative hook. Lead with it.
- The triple-R typo (`Terrraium2010.sln`) is comic relief gold. Use it every time.

📌 Sprint 1 blog (2025-07-16): Wrote `docs/blog/journal/01-server-bootstrap.md` covering: ASMX→Minimal API migration with before/after code, throttle middleware port (ASP.NET Cache→IMemoryCache), SQL Server as Aspire resource, Orleans+SignalR hybrid recommendation (4 grain types), Badger joining as Diagram Designer, Mike's HttpClient service layer, Hank's proactive testing strategy. Updated `docs/blog/announcement.md` with Sprint 1 progress section.

📌 Learnings (Sprint 1):
- The throttle port is a great "honoring the original devs" story — they found a creative solution with eviction callbacks. Frame it as respect, not ridicule.
- The Orleans discovery is the best narrative hook in Sprint 1. "You built actors before you knew what actors were" — that's the kind of reveal that makes migration stories compelling.
- Brady's VB.NET directive is important for tone: never dismissive, always respectful. "We're C# now" is the framing. This applies to all legacy tech, not just VB.NET.
- The before/after pattern (ASMX `[WebMethod]` → Minimal API lambda) works just as well for server code as it did for OrganismBase in Sprint 0.

📌 Sprint 2 blog (2025-07-16): Wrote `docs/blog/journal/02-configuration-core-infra.md` covering: GameConfig static monolith → IOptions<ServerSettings>, QueryPerformanceCounter P/Invoke → Stopwatch, PeerDiscovery heartbeat porting (ASMX with DataSet out params → typed HttpClient + JSON), Watson/BugService error reporting (DataSet over SOAP → POCO + Dapper), ErrorLog/Mutex → ILogger<T>, SignalR hub as deliberate thin layer (no business logic — Orleans owns that), OpenTelemetry + TerrariumTelemetry custom counters replacing Windows PerformanceCounter, Hank's code coverage + Configuration tests. Updated `docs/blog/announcement.md` with Sprint 2 progress section.

📌 Learnings (Sprint 2):
- The "they built what the framework didn't provide" framing is the strongest narrative for infrastructure migration stories. The Terrarium team's custom abstractions were proto-framework features. Frame it as validation, not obsolescence.
- QueryPerformanceCounter → Stopwatch is the most visceral single-line migration. The overhead compensation algorithm in the constructor — calling QPC twice to measure its own cost — is the kind of detail that makes devs go "oh wow." Lead with the human story of why they needed it.
- The BugService TODO is a great narrative detail — someone wrote TODO twenty years ago and now we finally closed it. Use callbacks to unfinished legacy work as storytelling hooks.
- The PerformanceCounter → OpenTelemetry comparison is gold for the "developer experience" angle. perfmon.exe over Remote Desktop vs. Aspire dashboard in your browser. That's the DX leap of two decades.
- The "deliberately thin hub" story works as a design principle lesson: explain what the code *doesn't* do and why. Absence of logic is itself an architectural decision worth documenting.

📌 Sprint 3 blog (2026-02-11): Wrote `docs/blog/journal/03-web-ui-foundation.md` covering: First Pixels — Blazor Interactive Server project with Glass-themed layout, component architecture (TerrariumViewport, CreaturePanel, EcosystemStatus, MessageLog), Glass CSS tokens rendering in browser (glass-theme.css + glass-components.css → ::before pseudo-element glass sheen), sprite preservation story (DirectDraw BMP sprite sheets, 48×48 frames, TerrariumSpriteSurfaceManager, size interpolation, skin family enums), Species registration migration (ASMX [WebMethod] Add → ISpeciesService + SpeciesServiceClient with async/JSON), SDK samples porting (.NET 10, creature developer experience with attribute-based point allocation), Sprint 2 recap (7 issues, 256 tests). Updated `docs/blog/announcement.md` with Sprint 3 progress section. PR #121.

📌 Learnings (Sprint 3):
- The "first pixels" moment is the strongest narrative hook for a UI sprint. The transition from invisible libraries to visible application is a psychological inflection point — lead with the emotional beat.
- The Glass CSS `::before` pseudo-element recreating the original GlassHelper.FillRectangle overlay is a great bridge story. Four lines of CSS replacing COM interop rendering. That's the kind of comparison that resonates.
- The sprite pipeline archaeology (BMP naming conventions, size variants, DirectDraw surfaces, animation frame grids) is rich material. The rendering system was sophisticated for 2003. Frame it as engineering respect.
- The creature developer experience (attribute-based point allocation, event-driven lifecycle) is the Terrarium story. The SDK samples are the onboarding ramp — without them, the SDK is just documentation. Always lead with "what does the developer actually write?"
- Brady's sprite directive ("people who know .NET Terrarium should recognize it immediately") is the visual north star. Recognition is the metric. Use it in every visual story.

📌 Sprint prep blog (2026-02-11): Wrote `docs/blog/journal/sprint-prep-the-road-ahead.md` — the "here's what's ahead" post capturing Sprints 7–13, 48 open issues, ~230 agent-minutes / ~89 wall-clock minutes, the eight-agent team, and the narrative hook: "an AI team is about to modernize a 25-year-old .NET app in under 2 hours of wall-clock time." Structured as: scope overview, sprint-by-sprint narrative (7 sprints: networking, renderer, shell, SDK, multi-peer, polish, launch), summary table of per-agent workload, and the emotional pitch ("this is the blog Hanselman gets").

📌 Learnings (Sprint prep):
- The "48 issues / 89 minutes" frame is visceral for engineers. Lead with the parallelism numbers, not the issue count. People understand wall-clock time better than agent-minutes.
- Sprint-by-sprint narrative > table. Each sprint needs a one-sentence "story" (e.g., "The nervous system. The internet pipes.") and 2–3 lines of human context before listing the issues.
- The per-agent workload table is critical context — it shows the team composition and makes it clear why parallelism matters. Skyler's 13 issues take 55 minutes sequentially; the whole team finishes in 89 minutes wall-clock.
- The emotional arc: (1) Foundation done, (2) here's what's ahead, (3) it's audacious, (4) we're about to do it, (5) watch it unfold. The post is a call-to-action to be present for the migration.
- Terrarium's 25-year history is the throughline. Open with recognition of what the project means to .NET community. Close with urgency ("Buckle up."). The blog IS about community stewardship, not just technical delivery.
