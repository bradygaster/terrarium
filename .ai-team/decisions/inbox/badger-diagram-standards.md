# Decision: Diagram Standards & Audit Results

**By:** Badger (Diagram Designer)
**Date:** 2025-07-16
**Status:** Implemented

---

## What Was Done

Full audit and upgrade of every diagram in every markdown file across the repository. All diagrams now meet Mermaid standards — no ASCII art remains for architectural or conceptual diagrams.

---

## Audit Results

### Converted from ASCII Art / Plain Code Blocks → Mermaid

| File | What Changed |
|------|-------------|
| `ARCHITECTURE.md` | Active Solution Dependency Graph — was a plain `` ``` `` code block listing standalone projects. Converted to `graph LR` with styled test→server relationship and note about empty shells. |
| `ClientWPF/ENGINE.md` | Teleportation Protocol — was ASCII pipe-and-dash sequence art. Converted to `sequenceDiagram` with proper steps, conditional assembly transfer, and labeled messages. |
| `.ai-team/decisions/inbox/heisenberg-orleans-evaluation.md` | Grain Types — was tree notation (├── └──) describing grain responsibilities. Converted to `graph TD` with subgraphs per grain type. This was an architecture diagram, not a file listing. |

### Quality Improvements (Existing Mermaid Upgraded)

| File | Diagram | Changes |
|------|---------|---------|
| `ARCHITECTURE.md` | Legacy Client Dependency Graph | Added descriptive arrow labels to all 17 edges (e.g., "runs simulation", "calls server", "applies theme") |
| `ClientWPF/README.md` | Rendering Pipeline | Added arrow labels ("provides COM types", "abstracts DirectX") |
| `ClientWPF/README.md` | App Structure | Added DockPanel position labels ("DockPanel.Top", "DockPanel.Left", etc.) |
| `ClientWPF/ENGINE.md` | Class Hierarchy | Fixed incorrect descriptions — Organism was labeled "mobile creatures" but it's the abstract base for both Animal and Plant |
| `docs/blog/journal/00-project-kickoff.md` | Target Architecture | Added descriptive arrow labels and added Server→Game edge |
| `.ai-team/decisions/inbox/heisenberg-orleans-evaluation.md` | Grain Interactions | Improved arrow labels with lowercase, consistent style |

### New Diagrams Added

| File | Diagram Type | What It Shows |
|------|-------------|---------------|
| `ClientWPF/ENGINE.md` | `graph LR` (flowchart) | 10-phase tick loop — visual flow from AI quantum (phases 0–4) through action gathering, combat, movement, reproduction, to state finalization |
| `ClientWPF/ENGINE.md` | `stateDiagram-v2` | Creature lifecycle — Born → Idle → actions (Moving, Attacking, Eating, etc.) → completion → back to Idle, with teleportation, death, and corpse rot paths |
| `ClientWPF/ENGINE.md` | `sequenceDiagram` | Teleportation protocol — 4-step handshake with conditional assembly transfer |

### Reviewed & Confirmed Good (No Changes Needed)

| File | Diagram | Verdict |
|------|---------|---------|
| `docs/blog/journal/00-project-kickoff.md` | Gantt chart (Sprint work streams) | Good — clear parallel streams with Primary/Support distinction |
| `MODERNIZATION.md` | Gantt chart (identical to blog post) | Good — matches the blog post version |

### Not Converted (Acceptable as-is)

Directory tree listings using `├──` and `└──` in the following files are **file/folder structure displays**, not architectural diagrams. These are standard markdown convention and are left as-is:
- `.github/agents/squad.agent.md`
- `docs/blog/README.md`
- `.ai-team/agents/scribe/charter.md`
- `.ai-team-templates/scribe-charter.md`

---

## Diagram Standards (Going Forward)

1. **No ASCII art. Ever.** All architectural, flow, sequence, state, and relationship diagrams use Mermaid.
2. **Directory trees are fine.** `├──` / `└──` for showing file structures is acceptable markdown convention.
3. **Every arrow gets a label.** No unlabeled edges — relationships must be explicit.
4. **Use subgraphs for system boundaries.** Group related components visually.
5. **PascalCase for node IDs, descriptive labels in brackets.** e.g., `GameEngine["Game Engine"]`
6. **Diagram type selection:**
   - Dependency/component relationships → `graph TD` or `graph LR`
   - Temporal sequences/protocols → `sequenceDiagram`
   - Lifecycles/state machines → `stateDiagram-v2`
   - Class hierarchies → `classDiagram`
   - Timeline/scheduling → `gantt`
7. **Color sparingly.** Use `style` or `classDef` only for semantic emphasis (e.g., highlighting the critical path or distinguishing empty shells from real code).
8. **Split when complex.** If a diagram needs more than ~15 nodes, consider splitting into multiple focused diagrams.
