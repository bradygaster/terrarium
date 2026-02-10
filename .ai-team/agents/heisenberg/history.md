# Project Context

- **Owner:** bradygaster (bradyg@microsoft.com)
- **Project:** .NET Terrarium 2.0 — peer-to-peer networked creature ecosystem game
- **Stack:** C#, .NET, WPF, ASP.NET MVC, DirectX, P2P networking
- **Created:** 2026-02-10

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

### 2025-07-15 — Solution Architecture Scan

- **Three generations coexist:** .NET 2.0 (Samples/SDK/Tools), .NET 3.5 (Client\ — the real code), .NET 4.0 (ClientWPF\/ServerMVC\ — abandoned rewrite shells).
- **The VS 2010 rewrite (`ClientWPF\`) was never completed.** 11 of 13 client projects contain only AssemblyInfo.cs. The actual game logic lives in `Client\`.
- **Active solution file:** `Terrraium2010.sln` (note the typo — three R's). Contains 15 projects in Server/Client solution folders.
- **Legacy solution files:** `Client\client.sln` (VS 2008 WinForms client), `Server\server.sln` (VS 2008 WebForms server).
- **No NuGet, no DI, no global build props.** All deps are framework assemblies or direct DLL references.
- **Strong naming** via `Keys\development.snk` on all legacy client assemblies.
- **Shared version info** via linked `Client\VersionInfo.cs`.
- **DirectX dependency** is the hardest modernization blocker — `Client\Renderer\` references `DXVBLib.dll` (DX7 COM interop).
- **Server communication** uses ASMX Web References in `Client\Services\` (BugReporting, Charts, Discovery, Messaging, Reporting, Species, Usage, Watson).
- **Legacy client dependency order (leaves first):** OrganismBase, Glass, Services → HttpListener, Configuration → Controls → Game → Renderer → Terrarium.
- **Key file paths:**
  - `ARCHITECTURE.md` — full solution architecture overview (created by me)
  - `Client\Terrarium\terrarium.csproj` — the main WinForms client executable (most project references)
  - `Client\Game\Game.csproj` — game engine (engine, hosting, P2P networking)
  - `Client\OrganismBase\OrganismBase.csproj` — organism SDK (actions, state, attributes, interfaces)
  - `Client\Renderer\Renderer.csproj` — DirectX rendering layer
  - `Client\Services\Services.csproj` — server communication via Web References
  - `Server\Website\` — original ASP.NET WebForms/ASMX server
  - `ServerMVC\TerrariumServer\` — MVC 2 scaffold (HomeController, AccountController only)

📌 Team update (2026-02-10): MVC Server is a scaffold — all game logic lives in legacy ASMX — decided by Gus
📌 Team update (2026-02-10): Build must be green before new tests — decided by Hank
