# Azure Container Apps Health Probes & Auto-Scaling

This document describes the health probe configuration and auto-scaling rules for Terrarium's Azure Container Apps deployment.

## Overview

Terrarium uses three types of health probes to ensure container reliability and enable intelligent auto-scaling:

1. **Liveness Probe** — Checks if the container is still running
2. **Readiness Probe** — Checks if the container can accept traffic
3. **Startup Probe** — Provides extra time for slow container startup

## Health Check Endpoints

Both `Terrarium.Server` and `Terrarium.Web` expose health check endpoints via the `ServiceDefaults` library:

### `/alive` — Liveness Check
- **Purpose**: Basic self-check that the application process is running
- **Implementation**: Simple HTTP 200 response from a "self" health check
- **Used by**: Liveness and Startup probes
- **Failure action**: Container is restarted

### `/health` — Readiness Check
- **Purpose**: Comprehensive check that all dependencies are ready
- **Implementation**: Checks all registered health checks (see below)
- **Used by**: Readiness probe
- **Failure action**: Container is removed from load balancer rotation

## Terrarium.Server Health Checks

The server performs the following health checks via `/health`:

| Check | Purpose | Tag | Failure Impact |
|-------|---------|-----|----------------|
| `self` | Process is running | `live` | Liveness failure → restart |
| `terrarium-db` | SQL Server connectivity | `ready` | Readiness failure → no traffic |

**Configuration**: `Terrarium.ServiceDefaults/Extensions.cs` → `AddDefaultHealthChecks()`

The health checks are conditionally registered based on connection string presence:
- If `ConnectionStrings:Terrarium` is set → SQL Server health check is added

**Note on SignalR**: Azure SignalR Service health is not explicitly checked because:
1. Standard health check packages don't support Azure SignalR Service (only self-hosted SignalR hubs)
2. SignalR connection failures are logged but non-fatal — the app can still serve HTTP traffic
3. If SignalR connectivity is critical, Azure Monitor tracks SignalR Service health independently

## Terrarium.Web Health Checks

The web frontend has simpler requirements:

| Check | Purpose | Tag | Failure Impact |
|-------|---------|-----|----------------|
| `self` | Process is running | `live` | Liveness failure → restart |

The web app doesn't directly connect to databases or SignalR, so it only needs a basic self-check.

## Probe Configuration (Bicep)

### Terrarium.Server Probes

```bicep
probes: [
  {
    type: 'Liveness'
    httpGet: {
      path: '/alive'
      port: 8080
      scheme: 'HTTP'
    }
    initialDelaySeconds: 10
    periodSeconds: 30
    timeoutSeconds: 5
    failureThreshold: 3
  }
  {
    type: 'Readiness'
    httpGet: {
      path: '/health'
      port: 8080
      scheme: 'HTTP'
    }
    initialDelaySeconds: 5
    periodSeconds: 10
    timeoutSeconds: 5
    failureThreshold: 3
  }
  {
    type: 'Startup'
    httpGet: {
      path: '/alive'
      port: 8080
      scheme: 'HTTP'
    }
    initialDelaySeconds: 0
    periodSeconds: 5
    timeoutSeconds: 5
    failureThreshold: 30
  }
]
```

**Startup Probe Strategy**:
- Checks every 5 seconds for up to 30 attempts (150 seconds total)
- Allows time for .NET assembly loading, dependency injection, and initial connections
- Once startup succeeds, liveness and readiness probes take over

**Liveness vs Readiness**:
- **Liveness** uses `/alive` (basic self-check) — checks every 30 seconds
- **Readiness** uses `/health` (full dependency check) — checks every 10 seconds
- This separation prevents aggressive container restarts when only dependencies are temporarily unavailable

### Terrarium.Web Probes

Same configuration as server, but simpler because it only checks basic process health.

## Auto-Scaling Rules

### Terrarium.Server Scaling

**Replica range**: 1-10 instances

**Scaling rules**:

1. **CPU-based scaling**
   - Trigger: CPU utilization > 70%
   - Action: Scale up
   - Reason: General compute load indicator

2. **SignalR connection-based scaling**
   - Trigger: SignalR connection count > 100
   - Metric: Azure Monitor `ConnectionCount` from SignalR Service
   - Action: Scale up
   - Reason: Terrarium's primary workload is WebSocket connections for real-time creature updates

**Configuration in Bicep**:

```bicep
scale: {
  minReplicas: 1
  maxReplicas: 10
  rules: [
    {
      name: 'cpu-scaling'
      custom: {
        type: 'cpu'
        metadata: {
          type: 'Utilization'
          value: '70'
        }
      }
    }
    {
      name: 'signalr-connection-scaling'
      custom: {
        type: 'azure-monitor'
        metadata: {
          metricName: 'ConnectionCount'
          metricResourceUri: signalR.id
          targetValue: '100'
        }
        auth: [
          {
            secretRef: 'signalr-connection'
            triggerParameter: 'connectionFromEnv'
          }
        ]
      }
    }
  ]
}
```

**Why SignalR-based scaling?**
- Terrarium's game state broadcasts are WebSocket-heavy
- SignalR connection count is the most direct indicator of active players
- Each server replica can handle ~100 concurrent SignalR connections reliably
- Azure SignalR Service tracks connection count per server via Azure Monitor

### Terrarium.Web Scaling

**Replica range**: 1-5 instances

**Scaling rules**:

1. **HTTP request-based scaling**
   - Trigger: Concurrent HTTP requests > 50
   - Action: Scale up
   - Reason: Web frontend is primarily request/response (page loads, API calls to server)

**Configuration in Bicep**:

```bicep
scale: {
  minReplicas: 1
  maxReplicas: 5
  rules: [
    {
      name: 'http-scaling'
      http: {
        metadata: {
          concurrentRequests: '50'
        }
      }
    }
  ]
}
```

## Aspire Integration

The `Terrarium.AppHost` project registers health checks for all resources:

```csharp
var sql = builder.AddSqlServer("sql", password: sqlPassword)
    .WithHealthCheck();

var signalR = builder.AddAzureSignalR("signalr")
    .WithHealthCheck();

var server = builder.AddProject<Projects.Terrarium_Server>("server")
    .WithHealthCheck();

builder.AddProject<Projects.Terrarium_Web>("web")
    .WithHealthCheck();
```

This enables:
- **Aspire Dashboard** health check visualization during local development
- **Dependency-aware startup ordering** via `.WaitFor()` — ensures dependencies are healthy before starting dependent services
- **Health check endpoint discovery** — Aspire automatically discovers `/health` and `/alive` endpoints

## Testing Health Probes Locally

### Using Docker

If running containers locally, test health probes with:

```bash
# Check liveness
curl http://localhost:8080/alive

# Check readiness (full health check)
curl http://localhost:8080/health

# Expected responses:
# /alive  → "Healthy"
# /health → JSON with check statuses
```

### Using Aspire Dashboard

1. Run `dotnet run --project src/Terrarium.AppHost`
2. Open Aspire Dashboard (URL shown in console)
3. Navigate to "Resources" → observe health status indicators
4. Click on a resource → "Health Checks" tab → see detailed check results

### Simulating Failures

**Test liveness failure** (container should restart):
```bash
# Stop the process → liveness probe fails → Container Apps restarts container
```

**Test readiness failure** (container removed from load balancer):
```bash
# Stop SQL Server → readiness probe fails → no new traffic routed
# Container stays alive (liveness still passes), just not serving requests
```

## Troubleshooting

### Container keeps restarting

- **Check**: Liveness probe (`/alive`) is failing
- **Causes**: App crash, unhandled exception in startup, port not listening
- **Solution**: Check container logs in Azure Portal or Log Analytics

### Container not receiving traffic

- **Check**: Readiness probe (`/health`) is failing
- **Causes**: DB connection string misconfigured, slow DB startup
- **Solution**: Check `/health` endpoint directly, verify connection strings in Azure Portal

### Slow startup causes restart loop

- **Check**: Startup probe is failing before the 150-second timeout
- **Causes**: Very slow assembly loading, slow DB migrations
- **Solution**: Increase `failureThreshold` in startup probe (each attempt is 5 seconds)

### Auto-scaling not triggering

- **Check**: Azure Monitor metrics for SignalR `ConnectionCount` and CPU utilization
- **Causes**: Metric not reaching threshold, scaling rule misconfigured, replica already at max
- **Solution**: Review Container App logs and Azure Monitor metrics in Azure Portal

## Related Configuration Files

- **Health checks**: `src/Terrarium.ServiceDefaults/Extensions.cs`
- **Bicep probes**: `infra/main.bicep` (both `serverApp` and `webApp` resources)
- **Aspire AppHost**: `src/Terrarium.AppHost/Program.cs`
- **NuGet packages**: `src/Terrarium.ServiceDefaults/Terrarium.ServiceDefaults.csproj`
  - `AspNetCore.HealthChecks.SqlServer`

## References

- [Azure Container Apps Health Probes](https://learn.microsoft.com/azure/container-apps/health-probes)
- [Azure Container Apps Scaling](https://learn.microsoft.com/azure/container-apps/scale-app)
- [ASP.NET Core Health Checks](https://learn.microsoft.com/aspnet/core/host-and-deploy/health-checks)
- [Aspire Health Check Integration](https://learn.microsoft.com/dotnet/aspire/fundamentals/health-checks)
