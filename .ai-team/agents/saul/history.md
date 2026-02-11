# Project Context

- **Owner:** bradygaster (bradyg@microsoft.com)
- **Project:** .NET Terrarium 2.0 — peer-to-peer networked creature ecosystem game, modernized as a cross-platform web app
- **Stack:** C#, .NET 10, .NET Aspire, Azure Container Apps, GitHub Actions, Docker
- **Created:** 2026-02-10

## Learnings

<!-- Append new learnings below. Each entry is something lasting about the project. -->

📌 Team update (2026-02-11): Beth's voice is fearless, developer-first, community-driven — decided by bradygaster

📌 Team update (2026-02-11): Diagram standards — Mermaid only, no ASCII art. All diagrams must use Mermaid — decided by Badger, bradygaster
📌 Team update (2026-02-11): VB.NET respectful framing — never refer to VB.NET negatively, just "we're C# now" — decided by bradygaster
📌 Team update (2026-02-11): Orleans + SignalR hybrid — Aspire integration (AddOrleans) and SignalR.Orleans backplane assigned to Saul in Sprint 7 — decided by Heisenberg
📌 Team update (2026-02-11): CI pipeline created (.github/workflows/build.yml) targeting src/Terrarium.sln — decided by Hank
📌 Team update (2026-02-11): CSS token naming: --glass-{category}-{element}-{modifier}, BEM for components — decided by Jesse
📌 Team update (2025-07-16): Solution uses classic .sln format (not .slnx); CS1591 suppressed during initial port — decided by Heisenberg

📌 Aspire AppHost created (2026-02-11): Created Terrarium.AppHost (Aspire 13.1.0 with AppHost.Sdk) and Terrarium.ServiceDefaults (OpenTelemetry 1.15.0, ServiceDiscovery 10.3.0, Http.Resilience 10.3.0). Both build clean on .NET 10.0.103. Aspire no longer requires a workload in .NET 10 — it's purely NuGet-based. ServiceDefaults project reference needs IsAspireProjectResource="false" in the AppHost csproj to avoid ASPIRE004 warning. Server and Web project references are commented out until those projects are created.
