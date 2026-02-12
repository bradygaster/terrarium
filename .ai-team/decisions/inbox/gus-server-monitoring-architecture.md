### 2026-02-11: Server monitoring architecture — Aspire-integrated health checks and System.Diagnostics.Metrics

**By:** Gus (Server Dev)

**What:** Server monitoring uses Aspire-integrated health checks with liveness/readiness tags, System.Diagnostics.Metrics for counters/gauges, and structured logging with ILogger.BeginScope() for consistent property names across all server components.

**Why:** 
- Health checks follow Aspire conventions (`/health` for readiness, `/alive` for liveness) with tag-based filtering
- System.Diagnostics.Metrics is the native .NET telemetry API — flows directly to Aspire dashboard without custom exporters
- Structured logging with consistent property names (`PeerId`, `EcosystemId`, `TickNumber`, `TeleportId`, `OrganismId`) enables filtering and correlation in logs
- TerrariumMetrics lives in ServiceDefaults (shared layer) so both Server and Net projects can reference it
- Custom health checks are tagged `["ready"]` for readiness probes, self-check is tagged `["live"]` for liveness
- DatabaseHealthCheck returns Degraded (not Unhealthy) until database layer is implemented — signals "not ready but not broken"
- Metrics use dimensional tags (e.g., `ecosystem_id`) for filtering in dashboards

**Implementation details:**
- Health checks: `DatabaseHealthCheck`, `SignalRHubHealthCheck`, `AssemblyCacheHealthCheck` — all implement `IHealthCheck`
- Metrics: 6 metrics (3 gauges, 3 counters) in `TerrariumMetrics` class
- Structured logging: log scopes in `TerrariumHub` and `PopulationTrackingService` methods
- Log property naming: semantic names (`PeerId` not `ConnectionId`, `TotalOrganisms` not `Total`)
- Metrics providers: lambda functions in `Program.cs` for peer count (`TerrariumHub.GetConnectedPeerCount()`) and species count (`IPopulationTrackingService.GetActiveSpeciesCount()`)
