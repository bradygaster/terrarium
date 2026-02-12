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

📌 Azure SignalR Service scaling architecture (2026-02-11): Integrated Azure SignalR Service as an Aspire resource for horizontal scaling in production deployments. AppHost configured with `AddAzureSignalR("signalr")` and emulator support for local dev (no Azure dependency). Server conditionally enables Azure SignalR when `ConnectionStrings__signalr` is present, enforcing `ServerStickyMode.Required` to preserve per-connection rate limit state across instances. Bicep infrastructure provisions `Microsoft.SignalRService/signalR` with Standard_S1 SKU (1,000 connections, $49/month) and configures Container Apps sticky sessions for affinity. Created comprehensive documentation (`docs/architecture/signalr-scaling.md`) with Mermaid diagrams showing local vs. production topology, cross-server message routing, capacity planning (S1→S10→S100 scaling path), failure modes, cost estimation, and operational characteristics. Current capacity: 1,000 concurrent connections, 1-3 server instances with auto-scaling based on HTTP concurrent requests. Teleportation and EcosystemTick broadcasts route correctly through Azure SignalR backplane with <500ms p99 latency target. Issue #78 (Sprint 11) complete.

📌 Container Apps health probes and auto-scaling (2026-02-11): Configured comprehensive health probes for both Terrarium.Server and Terrarium.Web Container Apps. Implemented three-tier probe strategy: (1) Liveness probe checks `/alive` every 30s for basic process health, (2) Readiness probe checks `/health` every 10s for dependency health (SQL Server connectivity via `AspNetCore.HealthChecks.SqlServer`), (3) Startup probe allows 150s (30 attempts × 5s) for slow container initialization. Updated ServiceDefaults to conditionally register SQL health check when `ConnectionStrings:Terrarium` is present. Azure SignalR health check NOT included — standard health check packages don't support Azure SignalR Service (only self-hosted hubs), and SignalR failures are non-fatal (app still serves HTTP). Configured auto-scaling rules in Bicep: Server scales 1-10 replicas based on CPU >70% and SignalR ConnectionCount >100 (via Azure Monitor metric); Web scales 1-5 replicas based on HTTP concurrent requests >50. AppHost integration enables health check visualization in Aspire Dashboard during local dev. Created comprehensive documentation at `docs/deployment/health-probes.md` covering probe configuration, health check endpoints, auto-scaling rules, testing procedures, and troubleshooting scenarios. Issue #86 (Sprint 12) complete.

📌 Production deployment readiness (2026-02-11): Created comprehensive deployment documentation and workflows for Terrarium production deployment. Delivered three key artifacts: (1) **Deployment Guide** (`docs/deployment/deployment-guide.md`) covering prerequisites (.NET 10, Azure CLI, azd, Docker), local development with Aspire orchestration, Azure deployment via `azd up`, custom domain configuration with SSL, environment variable management, monitoring with Azure Monitor/Application Insights, GitHub Actions CD pipeline setup, and troubleshooting for common deployment issues. (2) **Deployment Checklist** (`docs/deployment/checklist.md`) providing a comprehensive pre-deployment, deployment, and post-deployment verification checklist covering infrastructure verification, authentication, code readiness, configuration, health checks, monitoring, security, GitHub Actions setup, OIDC configuration, and sign-off process. (3) **GitHub Actions Deployment Workflow** (`.github/workflows/deploy.yml`) automating the full CI/CD pipeline: build, test, and deploy to Azure using `azd deploy` with OIDC passwordless authentication, triggered on push to main or manual dispatch. Infrastructure files verified: `azure.yaml` correctly defines server and web services as Container Apps; `infra/main.bicep` provisions all required resources (Container Apps Environment, Log Analytics, SQL Database, Azure SignalR Service, two Container Apps with health probes and auto-scaling). Actual `azd up` deployment deferred to Brady with Azure subscription — all steps documented for execution. Issues #88 (deployment guide) and #90 (production readiness) complete. Ready for production deployment.

