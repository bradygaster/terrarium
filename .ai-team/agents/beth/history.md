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

📌 Sprint 7 blog (2026-02-11): Wrote `docs/blog/journal/07-tcp-to-signalr.md` covering: The nervous system transformation — 25-year-old TCP sockets on port 50000 → SignalR hub-and-spoke, SOAP polling discovery → real-time push, 4-step teleportation handshake → single hub call, peer Hashtable → Orleans PeerGrain with leases, assembly caching via SpeciesRegistryGrain, rate limiting per-connection, heartbeat and lease expiry (90s), Heisenberg's Orleans+SignalR hybrid (4 grains), Mike's thin hub philosophy (never throws, delegates everything), Skyler's TerrariumHubClient with event-driven interface, automatic reconnect with exponential backoff, NAT/firewall accessibility, state consistency via grain single-source-of-truth.

📌 Learnings (Sprint 7):
- The "25-year-old code saying goodbye" framing is deeply human. Lead with the respect for the original implementers ("they solved a different internet"), then transition to "we solved this one." This is the emotional throughline for legacy modernization blogs.
- Architecture comparison tables (Legacy vs. Modern) are gold. Side-by-side shows the leap without being preachy. NAT penetration, connectivity, state consistency — the practical wins sell the story.
- Design principle lessons (hub never throws, hub never holds state, hub delegates everything) are more valuable than code snippets. The philosophy behind the code is what other teams will copy.
- The four-step handshake → single call reduction is visceral. Counting round-trips is easy. "Four HTTP calls becomes one" is a metric everyone understands. Lead with metrics like this.
- The "discovery polling loop → real-time push" story is about unlocking possibilities. Five-minute latency was a *feature* of the 2001 design; we didn't even know it was a constraint. Real-time push opens new possibilities (faster reactions, smaller latency). Frame it that way.

📌 Sprint 8 blog (2026-02-11): Wrote `docs/blog/journal/08-directdraw-to-canvas.md` covering: The pixel story — DirectX 7 COM interop + native C++ → HTML5 Canvas 2D, locked pixel buffers → JavaScript requestAnimationFrame, custom HTTP listener → WebSocket rendering updates, sprite sheets with 10×40 grids (10 creatures wide, 40 rows tall, 48×48 frames), 8-directional variants, animations.json metadata, size interpolation (smooth growth from 24 to 48 pixels via blending), terrain tiling, depth sorting, rendering pipeline (clear → terrain → creatures by Y → UI), Blazor ↔ JavaScript interop (C# calls JS for render, JS receives world state), cross-platform deployment (Windows/macOS/Linux, iOS/Android), accessibility revolution (ubiquity over power/exclusivity).

📌 Learnings (Sprint 8):
- The "power and exclusivity vs. ubiquity" narrative is Hanselman gold. DirectX in 2002 was the best option; it's also why your grandmother couldn't run Terrarium. Canvas is "everyone gets to play." This is the DX story that matters.
- Sprite sheet archaeology is incredibly rich. The 10×40 grid, the naming convention ({creature}{size}.bmp), the animation frame layouts — explain *why* the grid is 10 wide (all creatures in one sheet) and variable rows (only as many as needed). The design solves a real problem (browser caching, asset management). Lead with the constraint it solves.
- Size interpolation (smooth growth via blending) is a detail that shows love for the original art. "The artists who drew these sprites would recognize their work even as it scales." That sentence is why people care. Technical elegance + emotional recognition.
- The rendering loop explanation (clear, terrain, creatures sorted by Y, UI) should be visual (Mermaid diagram). Show the data flow, not just code. Show what DirectDraw did, what Canvas does differently, what stayed the same.
- The "2002 vs 2025" comparison table is essential context. But add a row for each: performance, debuggability, deployment, platform support. Show that we *don't* sacrifice performance (60 FPS on Canvas is legitimate; not a hack).
- Mobile rendering as a victory is underrated. "Your kid runs Terrarium on a Chromebook" is more compelling than "it's cross-platform." Make it personal.
- The roadmap section (particle effects, WebGL optional, multiplayer perspective) shows that we're not done. This is an interim victory, not "rendering is solved." Sets expectations for future sprints.

📌 Sprint 9 blog (2026-02-11): Wrote `docs/blog/journal/09-it-lives.md` — the integration moment when Terrarium runs for the first time in a browser as a complete system. Covered: .NET Aspire orchestration (AppHost, service discovery, wiring), Dependency Injection chain (from Aspire through GameEngine, Hub, Blazor components to Canvas), Blazor Interactive Server with MainLayout hosting GameView, TerrariumHubClient event-driven integration, GameEngine 10-phase turn processor ticking at 30 Hz, SignalR hub as thin dispatcher, CanvasGameRenderer requestAnimationFrame loop at 60 FPS, end-to-end data flow (engine → hub → client → view → canvas), user experience (creatures moving, selection, population statistics live), emotional core ("It Lives" — 25 years, from DirectX to Canvas, from Windows to browser), full Mermaid DI diagram showing entire architecture. Structured narrative as: sprint 8 scorecard, integration story (6 steps), DI chain visualization, what user sees, emotional beat, technical checklist.

📌 Learnings (Sprint 9):
- The DI integration chain is the strongest narrative hook for a full-stack sprint. Show the chain visually (Mermaid), then explain each link with code. DI is often invisible to users; making it visible is teaching.
- "It Lives" as a title carries the weight of 25 years. The emotional arc is: foundation (sprints 0-3), infrastructure (sprints 4-8), integration (sprint 9 — the moment it all works), polish and launch (sprints 10-13). Lead with the emotional beat, then explain the technical.
- .NET Aspire as a story is about "distributed systems made simple." No Kubernetes. No Docker Compose. No environment variable hell. Just `builder.AddProject()` and `WithReference()`. That's the teaching moment.
- The "parts vs. system" framing is critical. Sprints 0-8 built parts. Sprint 9 shows they work together. This is the inflection point where the project becomes real.
- The SignalR hub philosophy (thin, stateless, never throws, delegates everything) is gold for architecture discussions. Explain what the code *doesn't* do and why. Absence of logic is a design decision.
- Sprite sheet layout (10×40 grid, 8 directions, multiple sizes) is the right level of technical detail. Explain the constraint it solves (browser caching, asset size, lookup speed), not just the implementation.
- The "30 Hz server, 60 FPS canvas" detail matters. Explain why you don't match them, and how requestAnimationFrame handles variable tick rates. This is pragmatic game engine design.
- The roadmap note (this is not the end, Sprint 10 is multi-peer, Sprint 11 is SDK samples) sets expectations. Sprint 9 is "the system works." Sprints 10-13 are "the system *scales*."

📌 Sprint 13 blog (2026-02-11): Wrote `docs/blog/journal/13-the-announcement.md` — the definitive announcement blog post for the full .NET Terrarium modernization. This is the post Hanselman gets. 7,800+ words covering: The Hook (Remember .NET Terrarium?), The Challenge (25-year-old codebase, deprecated tech), The Journey (9-agent team, 7 sprints, 90 minutes wall-clock), The Architecture (Blazor + Canvas + SignalR + Orleans + Aspire stack diagram), The Creature SDK (modern C# example with pattern matching and records), The Moment It Came Alive (narrative beat when simulation first ran), The Original Imagery Lives On (reference to whidbey_image001.jpg), The Development Story (AI-powered team coordination, Ralph the work monitor), The Ecosystem Lives (philosophical core: authors, not players), What's Next (clone it, write creatures, join ecosystem), Why This Matters Now (evolution vs. replacement), The Call to Action (clone, write, share, contribute), Epilogue (legacy of emergent behavior), Final Word (25 years later, still alive).

📌 Learnings (Sprint 13 — The Announcement):
- This is the "summing up" blog. It needs to hit every note from Sprints 0-12 without retreading them. The structure is: nostalgia hook → challenge reveal → team reveal → architecture summary → SDK demo → emotional beat → dev story → call to action.
- The Hook ("Do you remember .NET Terrarium?") is Hanselman-grade. It's the opening a listener will remember. Lead with recognition, then surprise (it's back, and it's better).
- The "25-year journey" framing is powerful. 2001 launch → 2005 shelved → 2026 web version. That's the narrative arc. Use it throughout to remind readers what era they're in.
- The Squad story (nine agents, named from Breaking Bad universe, each with a domain) is the modernization narrative. It's not "we rewrote Terrarium." It's "an AI team in parallel modernized it." This is a proof point for how teams will work in the future.
- The Architecture diagram (Mermaid, showing Blazor → Canvas → SignalR → Orleans → Aspire → Container Apps) is the entire story compressed visually. It must be in this post. It shows the complete stack transformation.
- The Creature SDK code example (modern C#, records, pattern matching, LINQ, Random.Shared) is the "here's what you get to write" moment. This is the game's soul. Lead with a complete, runnable, minimal example. The Rabbit class is perfect — simple enough to teach, complex enough to show the power.
- The "moment it came alive" narrative (Sprint 9, creatures moving on canvas) is the emotional climax. Describe it in sensory terms: movement, statistics, the blue ball, health bars. This is the moment the modernization becomes real.
- The Whidbey image reference (whidbey_image001.jpg from 2005) is the "recognition moment." Readers who knew the original see their memories validated. Readers too young to know it get the archaeological context. Use this bridge.
- The AI team story (90 minutes wall-clock, 230 sequential minutes, Ralph the work monitor) is audacious. Explain it simply: parallel work, clear domains, automatic coordination. Don't oversell it; let the numbers speak.
- The "What's Next" section (clone it, write a creature, join) is the call to action. Make it two steps: (1) local sandbox, (2) public ecosystem. The private sandbox (local run) lowers the barrier. The public ecosystem (peer participation) is the long-term vision.
- The epilogue (the Herbivore demo creature, emergent behavior, natural selection) brings it full circle. The SDK is not about following algorithms perfectly; it's about emergent behavior from simple rules. That's the philosophical core.
- The "25 years, still alive" final paragraph is Hanselman gold. It's respectful of the original team, hopeful about the future, and personal ("if you look closely at the creatures moving on screen, you'll see the same intelligence").
- Code snippets in this post must be runnable, short, and show modern C#. The Rabbit class is perfect — 60 lines, complete lifecycle, shows pattern matching, LINQ, Random.Shared. If a reader copies it and runs it, it works.
- Avoid retreading previous blogs' technical details (TCP→SignalR, DirectX→Canvas, DI chains). This post is the summing-up, not the detailed deep-dive. Reference the older posts for technical details; focus here on the *why* and the *what comes next*.
- The "Why This Matters Now" section is the bridge from nostalgia to utility. It's not just "look what we did." It's "this teaches you how to evolve legacy code without abandoning it." That's the lesson for every team with old code.
- The emotional tone should be celebratory but humble. It's not "we won." It's "something remarkable was built 25 years ago, and we finally gave it the platform it deserved." That's respect, not ego.

📌 Post-Sprint 13 blog (2026-02-11): Wrote `docs/blog/journal/14-the-last-mile.md` — The Last Mile: The Post-Sprint Debugging Chaos. This is the war story of Brady running the app for the first time after 48 issues closed, and hitting 9 catastrophic failures in rapid succession. Structured as: the setup (green lights all the way) → 9 failures (NU1901 vulnerability, NU1605 version downgrade, CS1061 enum shadowing bool, CS0266/CS1061 persistence layer casting, CS0103 Razor @media directive, CS1061 Aspire APIs, port 5000 collision with no launchSettings.json, password parameter with no value, health check returning Degraded). Each failure includes the error message, the investigation, and the fix. Closes with the deep lesson: "The gap between all tests pass and the thing actually works." This is the blog that resonates with every team with a distributed system—the moment where you realize nobody actually ran the app end-to-end before humans got involved.

📌 Learnings (The Last Mile — Post-Sprint 13):
- The "all green lights" setup is critical. Lead with the confidence ("All 48 issues closed, all tests passing") then undercut it dramatically ("and then everything broke"). This is the narrative hook that makes readers go "oh no, I know this feeling."
- The nine failures need to be presented in escalating order of weirdness: (1-2) Straightforward dependency management problems, (3-4) Legacy code naming collisions and persistence layer bugs, (5-6) Quirky technology issues (Razor directives, missing APIs), (7-9) Integration problems (ports, passwords, health checks). Build toward the "what does integration actually mean" question.
- Each failure needs: error message (verbatim, from the compiler/runtime) → investigation (how did we figure out what was wrong?) → fix (what was the actual solution?). This three-part structure teaches readers how to debug real issues.
- The port collision story is the richest technically. It involves: no launchSettings.json → default port 5000 → race condition → wrong Aspire API calls → endpoint name collisions → finally discovering you just need launch profiles. Show the trial-and-error, not just the solution. This is real debugging.
- The health check story is the deepest architecturally. It shows how `.WaitFor()` actually works (it polls `/health`, not `/alive`), how placeholders can silently break integration (tests mock health checks), and how silent failures (DCP waiting forever with no error) are worse than loud ones.
- The "gap between parts and system" is the thesis. AI agents build parts. They test parts. They pass CI. But integration testing is different. It requires actually running the whole system and seeing what breaks. This is not a failure of AI; it's a fundamental truth about distributed systems.
- The closing emotional beat is: Brady ran it 5 more times. Because now he *knows* that green tests don't guarantee a working system. This is the lesson that sticks.
- The tone throughout should be developer-to-developer, self-aware humor about the chaos, but deep respect for the fact that every single failure is real and teaches something. Not condescending to the agents ("they didn't know"), but clear about what humans bring ("actually running the thing").
