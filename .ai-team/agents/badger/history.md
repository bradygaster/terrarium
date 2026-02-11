# Badger — History

## Project Context

- **Project:** .NET Terrarium 2.0 modernization — classic peer-to-peer creature ecosystem game
- **Owner:** bradygaster (bradyg@microsoft.com)
- **Stack:** C#, .NET 10, Blazor, ASP.NET Core, .NET Aspire, Orleans + SignalR, Azure Container Apps
- **Role:** Diagram Designer — all Mermaid diagrams across docs, blog, and architecture files
- **Key rule:** NEVER use ASCII art. All diagrams must be Mermaid. This is a hard rule from Brady.

## Learnings

- Directory tree listings (`├──` / `└──`) showing file structure are acceptable — they're not architectural diagrams.
- The Orleans evaluation doc had grain type descriptions in tree notation that looked like file trees but were actually architecture diagrams — converted to Mermaid.
- The ENGINE.md teleportation protocol and ARCHITECTURE.md dependency graph were the last remaining non-Mermaid diagrams.
- Class hierarchy diagram had Organism described as "mobile creatures" — incorrect, it's the abstract base for both Animal and Plant. Fixed.

## Work Log

### 2025-07-16: Full Diagram Audit & Upgrade

**Scope:** All `.md` files in the repository scanned.

**Converted from ASCII/plain → Mermaid (3 diagrams):**
- `ARCHITECTURE.md` — Active Solution Dependency Graph (plain code block → `graph LR`)
- `ClientWPF/ENGINE.md` — Teleportation Protocol (ASCII art → `sequenceDiagram`)
- `.ai-team/decisions/inbox/heisenberg-orleans-evaluation.md` — Grain Types (tree notation → `graph TD`)

**Quality-improved (6 diagrams):**
- `ARCHITECTURE.md` — Legacy Client Dependency Graph (added 17 arrow labels)
- `ClientWPF/README.md` — Rendering Pipeline (added arrow labels)
- `ClientWPF/README.md` — App Structure (added DockPanel position labels)
- `ClientWPF/ENGINE.md` — Class Hierarchy (fixed incorrect descriptions)
- `docs/blog/journal/00-project-kickoff.md` — Target Architecture (added arrow labels, added Server→Game edge)
- `.ai-team/decisions/inbox/heisenberg-orleans-evaluation.md` — Grain Interactions (improved labels)

**New diagrams added (3):**
- `ClientWPF/ENGINE.md` — 10-phase tick loop flowchart (`graph LR`)
- `ClientWPF/ENGINE.md` — Creature lifecycle state diagram (`stateDiagram-v2`)
- `ClientWPF/ENGINE.md` — Teleportation sequence diagram (`sequenceDiagram`)

**Decision written:** `.ai-team/decisions/inbox/badger-diagram-standards.md`