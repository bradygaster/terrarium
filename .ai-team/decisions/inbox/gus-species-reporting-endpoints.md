# Decision: Species & Reporting Endpoint Design

**From:** Gus (Server Dev)
**Date:** 2025-07-16
**PR:** #120
**Issues:** #26, #27

## Decisions Made

### 1. Assembly storage deferred
The legacy `SpeciesService` stored creature assemblies to disk using `FileIOPermission` (Code Access Security). CAS doesn't exist in .NET 10. Assembly storage needs a separate design decision — likely Azure Blob Storage or a configurable filesystem path without CAS. The `GetSpeciesAssembly` endpoint and `SaveAssembly`/`LoadAssembly`/`RemoveAssembly` methods are not ported. The register endpoint inserts the DB record but does not persist the assembly binary.

### 2. Word filter (PoliCheck) deferred
The legacy register checked species names, author names, and emails against `invalidwordlist.txt` via `WordFilter.RunQuickWordFilter()`. This is not ported yet — it depends on a static file and the `WordFilter.cs` utility. When ported, it should be a service injected into the endpoint, not a static call.

### 3. Charts consolidated under /api/reporting/stats/
Rather than creating a separate `/api/charts/` route group, chart data endpoints live under `/api/reporting/stats/`. The legacy `ChartService.asmx` was a thin SOAP wrapper around `ChartBuilder.cs` which just ran stored procedures. The three chart endpoints (`species-list`, `latest`, `top-animals`) are data queries — they belong with reporting.

### 4. ReportPopulation returns Success on server errors
Matches legacy behavior documented in the original code: "Return success instead of ServerDown because, if the server is getting hammered, Success will tell clients to stop retrying, where ServerDown will tell them to keep doing it."

## Impact
- **Mike/Jesse (client devs):** Species API contract is defined. When the client needs to register/list/reintroduce species, these are the endpoints.
- **Hank (tester):** 9 new endpoints to cover. Integration tests will need a SQL Server container with the init script.
- **Saul (DevOps):** No new infrastructure needed — endpoints use the existing `SpeciesDsn` connection string from `ServerSettings`.
