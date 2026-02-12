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
📌 Team update (2026-02-11): Aspire packages pinned — Aspire 13.1.0, ServiceDiscovery/Resilience 10.3.0, OpenTelemetry 1.15.0 — decided by Saul
📌 Team update (2026-02-11): CSS token naming: --glass-{category}-{element}-{modifier}, BEM for components — decided by Jesse
📌 Team update (2026-02-11): Services layer (HttpClient-based, interface-first, no ServiceDefaults dependency) — decided by Mike
📌 Team update (2026-02-11): SignalR Hub contract (8 methods, 7 callbacks, rate limiting, error struct) — decided by Mike
📌 Team update (2026-02-11): Terrarium.Web Blazor Interactive Server (PR #118) — decided by Skyler
📌 Team update (2026-02-11): Glass CSS expanded (60+ tokens, 76 assets cataloged) — decided by Jesse
📌 Team update (2026-02-11): Server.Tests (17 xUnit tests) — decided by Hank
📌 Team update (2026-02-11): SDK Samples (standalone structure) — decided by Hank
📌 Team update (2026-02-11): Species & Reporting endpoints — decided by Gus
📌 Team update (2026-02-11): Organism Isolation architecture (3-layer sandbox) — decided by Heisenberg
📌 Team update (2026-02-11): Hub-and-spoke SignalR architecture (rate limits, heartbeat/lease, reconnect=rejoin) — decided by Heisenberg
📌 Team update (2026-02-11): Road ahead blog post (48 issues, 89-minute parallelism) — decided by Beth

📌 Aspire AppHost created (2026-02-11): Created Terrarium.AppHost (Aspire 13.1.0 with AppHost.Sdk) and Terrarium.ServiceDefaults (OpenTelemetry 1.15.0, ServiceDiscovery 10.3.0, Http.Resilience 10.3.0). Both build clean on .NET 10.0.103. Aspire no longer requires a workload in .NET 10 — it's purely NuGet-based. ServiceDefaults project reference needs IsAspireProjectResource="false" in the AppHost csproj to avoid ASPIRE004 warning. Server and Web project references are commented out until those projects are created.

📌 NuGet packages for OrganismBase (2026-02-11): Created Terrarium.OrganismBase NuGet package (version 10.0.0-preview.1) with full package metadata, README, and symbol package generation. Created Terrarium.Templates package with dotnet new template for creature scaffolding. Template supports Animal (herbivore/carnivore) and Plant types with parameterized customization. Created CI/CD workflow (.github/workflows/nuget-publish.yml) for automated publishing to GitHub Packages on release tags. Both packages build and pack successfully. Template tested with all variations — herbivore, carnivore, and plant generation all work correctly. Documentation added to docs/nuget-packages.md covering installation, usage, publishing, and troubleshooting.

