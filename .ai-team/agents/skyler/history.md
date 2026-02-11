# Project Context

- **Owner:** bradygaster (bradyg@microsoft.com)
- **Project:** .NET Terrarium 2.0 — peer-to-peer networked creature ecosystem game, modernized as a cross-platform web app
- **Stack:** C#, .NET 10, Blazor, ASP.NET Core, .NET Aspire, SignalR, Canvas/WebGL
- **Created:** 2026-02-10

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

📌 Team update (2026-02-11): Beth's voice is fearless, developer-first, community-driven — decided by bradygaster

📌 Team update (2026-02-11): Diagram standards — Mermaid only, no ASCII art. All diagrams must use Mermaid — decided by Badger, bradygaster
📌 Team update (2026-02-11): VB.NET respectful framing — never refer to VB.NET negatively, just "we're C# now" — decided by bradygaster
📌 Team update (2026-02-11): Orleans + SignalR hybrid recommended — 4 grain types for networking layer — decided by Heisenberg
📌 Team update (2026-02-11): CI pipeline created (.github/workflows/build.yml) targeting src/Terrarium.sln — decided by Hank
📌 Team update (2026-02-11): CSS token naming: --glass-{category}-{element}-{modifier}, BEM for components — decided by Jesse

## Sprint 3 Work

### 2026-02-11: Created Terrarium.Web Blazor Interactive Server project (#22, #24)
- **Branch:** `squad/22-blazor-web`
- **PR:** #118
- **What I built:**
  - Full Blazor Interactive Server project scaffold in `src/Terrarium.Web/`
  - `Program.cs` with AddServiceDefaults, AddSignalR, MapRazorComponents + InteractiveServerRenderMode
  - `App.razor` referencing glass-theme.css, glass-components.css, app.css
  - `Routes.razor` with MainLayout default
  - `MainLayout.razor` — Glass chrome layout (titlebar + viewport body + statusbar with LED)
  - `_Imports.razor` with standard Blazor usings
  - Four reusable Blazor components:
    - `TerrariumViewport.razor` — Canvas-based game viewport placeholder
    - `CreaturePanel.razor` — Sidebar with creature stats (species, energy, age, position)
    - `EcosystemStatus.razor` — Header metrics bar with LED indicators (creature count, tick, peers)
    - `MessageLog.razor` — Scrollable timestamped event log
  - `app.css` with layout styles using Glass tokens throughout
  - Updated AppHost to reference and serve Terrarium.Web with external HTTP endpoints
  - `.csproj` references ServiceDefaults
- **Build:** `dotnet build src/Terrarium.sln` — all green
- **Glass theme integration:** All components use `--glass-*` CSS tokens and `.glass-*` BEM classes from Jesse's theme system. No hardcoded colors.
