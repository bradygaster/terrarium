# Project Context

- **Owner:** bradygaster (bradyg@microsoft.com)
- **Project:** .NET Terrarium 2.0 — peer-to-peer networked creature ecosystem game
- **Stack:** C#, .NET, WPF, ASP.NET MVC, DirectX, P2P networking
- **Created:** 2026-02-10

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2025-07-15 — Initial Domain Mapping (ClientWPF)

**Key finding: All `ClientWPF/` projects are empty scaffolds.** Every project contains only `Properties/AssemblyInfo.cs` (and for TerrariumClient, empty `App.xaml`/`MainWindow.xaml`). No business logic has been ported.

**The real implementation lives in `Client/`** — specifically:
- `Client/Renderer/` — Full DirectDraw rendering pipeline (~25 .cs files). The main class is `TerrariumDirectDrawGameView` (a `PictureBox` subclass).
- `Client/Glass/` — Custom WinForms theming framework (GlassPanel, GlassButton, GlassLabel, GlassStyleManager).
- `Client/Controls/` — WinForms custom controls (TitleBar, BottomPanel, DeveloperPanel, StatusBar, TickerBar, LEDs).
- `Client/ControlsWPF/` — **Partial WPF port** of controls. Has real XAML UserControls (GlassTitleBar, GlassBottomPanel, DeveloperPanel, TickerBar) with a shared ResourceDictionary for Glass styling. Includes 18 PNG button icons.
- `Client/TerrariumWPF/` — **Partial WPF port** of the main window. `MainForm.xaml` shows the target DockPanel layout; `MainForm.xaml.cs` creates `TerrariumDirectDrawGameView` and hosts it via `WindowsFormsHost`.
- `Client/Terrarium/` — Legacy WinForms main app with UI classes (MainForm, SplashScreen, TraceWindow, PropertySheet, etc.).

**Architecture observations:**
- Rendering pipeline: `DXVBLib` (COM interop) → `Renderer/DirectX/` (managed wrappers) → `TerrariumDirectDrawGameView` (game loop). The `Graphics` project and `IGraphicsEngine` interface are both empty — DX7 is used directly.
- WPF interop via `WindowsFormsHost` embedding the WinForms `PictureBox`-based game view. This causes WPF airspace issues.
- All projects target .NET Framework 4.0 (VS2010-era MSBuild). TerrariumClient is x86-only (required for DX7 COM interop).
- No project references exist in `ClientWPF/` yet — none of the skeleton projects reference each other.
- The Glass styling system (WinForms) maps to WPF `ResourceDictionary` styles. The legacy `ControlsWPF/ResourceDictionary.xaml` has `GlassGradient`, `GlassLabel`, `GlassButton`, and `GlassButtonTemplate` already defined.

**File inventory written to:** `ClientWPF/README.md`

📌 Team update (2026-02-10): MVC Server is a scaffold — all game logic lives in legacy ASMX — decided by Gus
📌 Team update (2026-02-10): Build must be green before new tests — decided by Hank
📌 Team update (2026-02-10): .NET 10 modernization sprint plan created — 14 sprints, WPF on .NET 10, Silk.NET OpenGL, gRPC P2P, Dapper+stored procs, process isolation, xUnit, System.Text.Json — decided by Heisenberg
📌 Team update (2026-02-11): Beth's voice is inspired by Beth Massi — fearless, developer-first, community-driven — decided by bradygaster
