### 2026-02-11: Container Apps health probes and auto-scaling configuration

**By:** Saul

**What:** Configured comprehensive health probes (liveness, readiness, startup) and intelligent auto-scaling rules for Azure Container Apps deployment. Server scales based on SignalR connections and CPU; Web scales based on HTTP concurrency.

**Why:** 
1. **Reliability**: Health probes enable Container Apps to automatically detect and restart failed containers (liveness) and remove unhealthy instances from load balancer rotation (readiness). Startup probes prevent restart loops during slow .NET assembly loading.

2. **Auto-scaling intelligence**: Scaling rules target the right metrics for each service — Server scales on SignalR connection count (primary workload indicator) and CPU, Web scales on HTTP concurrent requests. This ensures efficient resource utilization and cost control.

3. **Production readiness**: Three-tier probe strategy (liveness/readiness/startup) follows Azure Container Apps best practices. SQL health check ensures database connectivity before accepting traffic. SignalR health check intentionally omitted (no reliable package support for Azure SignalR Service, non-fatal if it fails).

**Configuration:**
- `infra/main.bicep`: Probe configuration for both serverApp and webApp resources
- `src/Terrarium.ServiceDefaults/Extensions.cs`: SQL Server health check registration
- `src/Terrarium.AppHost/Program.cs`: Aspire health check integration (`.WithHealthCheck()`)
- `docs/deployment/health-probes.md`: Complete documentation with testing guidance

**Scaling thresholds:**
- Server: 1-10 replicas, scale on CPU >70% or SignalR connections >100
- Web: 1-5 replicas, scale on HTTP concurrent requests >50

**Dependencies:**
- `AspNetCore.HealthChecks.SqlServer` 8.0.2 added to ServiceDefaults
- Requires Azure Monitor access for SignalR connection count metric
