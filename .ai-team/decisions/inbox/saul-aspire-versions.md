# Aspire Package Versions for .NET 10

**By:** Saul (DevOps / Aspire)
**Date:** 2026-02-11
**Status:** Implemented

## What
Aspire AppHost and ServiceDefaults use these package versions:
- **Aspire.Hosting.AppHost** + **Aspire.AppHost.Sdk**: `13.1.0`
- **Microsoft.Extensions.ServiceDiscovery**: `10.3.0`
- **Microsoft.Extensions.Http.Resilience**: `10.3.0`
- **OpenTelemetry.\***: `1.15.0`

## Why
- Aspire workload is deprecated in .NET 10 — everything is NuGet-based now
- These are the latest stable versions as of 2026-02-11
- All other projects (Server, Web) should reference ServiceDefaults and call `builder.AddServiceDefaults()` + `app.MapDefaultEndpoints()`
- When Server and Web projects are created, uncomment the ProjectReference and AddProject lines in AppHost
