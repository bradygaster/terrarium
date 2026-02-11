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
📌 Team update (2025-07-15): CSS tokens use `--glass-{category}-{element}-{modifier}` naming; BEM classes; `glass-theme.css` is single source of truth — decided by Jesse

📌 Sprint 0 blog (2026-02-11): Wrote `docs/blog/journal/00-sprint-0-complete.md` covering the full foundation wave — 5 agents, 5 PRs, Mike's 91-file OrganismBase port, Jesse's Glass CSS tokens, Heisenberg's solution structure, Saul's Aspire AppHost, Hank's CI pipeline. Updated `docs/blog/announcement.md` with real Sprint 0 details in "The Challenge" and "The AI Story" sections.

📌 Learnings:
- Before/after code snippets are the most powerful storytelling tool for a migration blog. `[Serializable]` → gone, `namespace OrganismBase { }` → `namespace OrganismBase;` — these are visceral for .NET devs who lived through these changes.
- The CSS token extraction story (Color.FromArgb → CSS custom properties) bridges the old world and the new in a way non-technical readers can follow too.
- Mike's `ArrayList` deferral decision is worth highlighting — pragmatic migration decisions (not changing interfaces until the implementing project is ported) are the kind of detail that makes a migration story credible.
- Five branches landing in parallel is a great narrative hook. Lead with it.
- The triple-R typo (`Terrraium2010.sln`) is comic relief gold. Use it every time.
