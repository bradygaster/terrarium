# Retrospective — 2026-02-12
**Facilitator:** Heisenberg
**Scope:** Full modernization (Sprints 7-13) + post-sprint fixes
**Requested by:** Brady

## What Went Well

- **48 issues closed across 7 sprints with 9 agents in ~90 minutes wall-clock.** The parallelized sprint model — frontend, backend, networking, testing, and DevOps running simultaneously — delivered throughput that sequential work never could. Sprint boundaries kept scope manageable.
- **Incremental modernization strategy paid off.** Leaf-to-root project ordering (OrganismBase → Game → Net → Services → Server → Web) meant each sprint built on stable foundations. Zero circular dependencies in the final 23-project solution.
- **SignalR hub-and-spoke architecture replaced legacy TCP P2P cleanly.** The ITerrariumHub/ITerrariumClient contract (8 methods, 7 callbacks) was defined upfront and survived through implementation without breaking changes.
- **Aspire orchestration simplified the dev experience dramatically.** `dotnet run --project src/Terrarium.AppHost` brings up the entire stack. No more manual server configuration.
- **Legacy code removal was clean.** 764 files deleted, all preserved in git history. The repo went from a confusing multi-generation (.NET 2.0/3.5/4.0) mess to a single modern .NET 10 solution.
- **SDK developer experience is real.** `dotnet new terrarium-creature` scaffolds a working creature in seconds. NuGet packaging, API docs, and tutorials are in place.
- **Post-sprint seeding reduction (2000+ organisms → 16) was the right call.** The initial numbers were demo-impressive but made the ecosystem unobservable and overwhelmed the client rendering pipeline on first connect.
- **Playwright integration tests caught real issues.** Moving from diagnostic-only to assertion-based E2E tests (8 tests covering canvas rendering, SignalR state, tick advancement) provides actual regression protection.

## What Didn't Go Well

- **SignalR connectivity had three independent bugs that shipped together.** Hub URL mismatch (`/terrarium` vs `/hubs/terrarium`), missing CORS policy, and hub never started (`StartAsync` not called). All three had to be present simultaneously for the connection to fail — and all three shipped.
- **The EcosystemSimulationWorker seeded 2000-3000 plants on startup.** This was wildly excessive for a demo simulation. Nobody questioned the numbers during the sprint, and Brady had to flag it manually.
- **Pre-existing NuGet package conflicts persisted across multiple sprints.** `Microsoft.Identity.Client 4.56.0` vulnerability warnings and `NU1605` downgrade errors were known but deferred repeatedly. They weren't blocking, but they created noise in every build.
- **Duplicate type collisions across agents.** Mike and Skyler both defined `CreatureStateData` in different files — caught at build time but shouldn't have happened. Contract types in shared projects (Terrarium.Net) need single ownership.
- **Sprint 12 error handling added `AddStandardResilienceHandler()` but forgot the package.** `Microsoft.Extensions.Http.Resilience` wasn't in `Terrarium.Services.csproj`. The code compiled in isolation (the method is an extension method) but failed at build.
- **No integration testing until post-sprint.** Playwright tests were added after sprints 7-13 completed. The SignalR connectivity bugs would have been caught earlier with even basic smoke tests.

## Root Causes

### 1. No integration test gate during sprints
Each agent validated their own work in isolation — Mike confirmed the hub mapped correctly, Jesse confirmed the client connected, Gus confirmed CORS was configured. But nobody ran the full stack end-to-end until Brady tried it. **The contract was correct; the wiring was wrong.**

### 2. Shared type ownership was implicit
When multiple agents work on the same shared project (Terrarium.Net), there's no mechanism to prevent duplicate definitions. The `CreatureStateData` collision happened because Mike added it for the server simulation and Skyler needed it for client rendering — both reasonable, neither coordinating.

### 3. Seeding numbers were developer-intuitive, not user-intuitive
Mike built a simulation that "looks alive" from a server log perspective (thousands of organisms, complex population dynamics). But from a user's perspective — watching a canvas render — 16 organisms are more observable and more interesting than 3000.

### 4. Build warnings treated as acceptable noise
`NU1901` and `NU1605` warnings were known from Sprint 9 onward but never prioritized because "the build passes." This eroded signal-to-noise ratio in build output and made it harder to spot new issues.

### 5. Missing package references not caught by per-file compilation
`AddStandardResilienceHandler()` compiled because C# extension methods resolve at the call site. The missing package only manifests when the full project graph is restored and linked. Agents building individual projects don't always catch cross-project dependency gaps.

## What Should Change

### 1. Add a smoke test gate to every sprint
Before marking a sprint complete, run the full stack (`dotnet run --project src/Terrarium.AppHost`) and verify at minimum: (a) the app starts, (b) SignalR connects, (c) the canvas renders something. This takes 60 seconds and would have caught all three connectivity bugs.

### 2. Establish explicit ownership of shared projects
`Terrarium.Net` (contracts, messages, DTOs) should have a designated owner — likely Mike (networking) — and any agent adding types to it should check for existing definitions first. A simple `grep` before creating a new file is sufficient.

### 3. Treat build warnings as CI failures
Add `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>` (already set) but also audit `<NoWarn>` entries quarterly. Suppressed warnings should have a comment explaining *why* they're suppressed and *when* the suppression can be removed.

### 4. Tune simulation parameters with the user, not for the server
Default seeding values should be conservative (what's observable on screen) with configuration to scale up. The EcosystemSimulationWorker should read initial counts from `IConfiguration` so they can be tuned without code changes.

### 5. Require full solution restore before marking work complete
Agents should run `dotnet build src/Terrarium.sln` (not just their project) before declaring their sprint work done. This catches missing package references and cross-project compilation errors.

### 6. Add telemetry/observability for ecosystem activity
Brady identified the need to blog about ecosystem activity. The infrastructure exists (OpenTelemetry, Application Insights, structured logging) but ecosystem-specific metrics (births, deaths, population curves, species diversity) aren't instrumented yet. This should be a near-term work item.

## Action Items

| Owner | Action |
|-------|--------|
| Hank | Add a "smoke test" Playwright spec that runs against AppHost and validates SignalR connection + canvas rendering — gate for all future sprints |
| Mike | Document ownership of `Terrarium.Net` shared project; add a `CODEOWNERS`-style comment to the `.csproj` |
| Mike | Make `EcosystemSimulationWorker` seeding counts configurable via `IConfiguration` (appsettings.json) |
| Gus | Add ecosystem-specific metrics to `TerrariumMetrics`: organism births, deaths, population by species, tick processing time |
| Heisenberg | Audit `<NoWarn>` entries in `Directory.Build.props` — add comments explaining each suppression and removal conditions |
| Saul | Update CI workflow (`build.yml`) to run `dotnet build src/Terrarium.sln` (full solution) rather than individual projects |
| Skyler | Before adding types to `Terrarium.Net`, grep for existing definitions in that project first |

## Metrics

| Metric | Value |
|--------|-------|
| **Sprints completed** | 7 (S7–S13) |
| **Issues closed** | 48 / 48 (100%) |
| **Agents involved** | 9 |
| **Decisions recorded** | 127+ |
| **Legacy files removed** | 764 |
| **Post-sprint bugs found** | 3 (SignalR URL, CORS, StartAsync) |
| **Post-sprint bugs found by tests** | 0 (all found manually by Brady) |
| **Post-sprint bugs found after adding Playwright** | 0 (all passed — tests added after fix) |
| **Time from "project complete" to "actually working E2E"** | ~1 session (SignalR fix + game loop wiring) |
| **Seeding reduction factor** | ~187× (2300 avg → 16 organisms) |
| **Defect injection pattern** | Cross-agent wiring (not logic errors within a single agent's code) |

**Key observation:** Every post-sprint bug was a *wiring* issue — not a logic error. The individual components were correct. The failures were at integration boundaries (URL paths, CORS policy, lifecycle calls, type duplication). This strongly argues for integration testing as the highest-leverage quality investment going forward.
