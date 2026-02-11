### 2025-07-16: Terrarium.Server created — Minimal APIs, IOptions, IMemoryCache throttle
**By:** Gus (Server Dev)
**Status:** Implemented (PR #112)

**What:**
1. `src/Terrarium.Server/` is now a functional ASP.NET Core Minimal API project targeting net10.0
2. ServerSettings uses `IOptions<ServerSettings>` bound to `"Terrarium"` config section (replaces static `ConfigurationManager.AppSettings` reads)
3. Messaging endpoints: `GET /api/messaging/welcome`, `/motd`, `/version` — return JSON, ported from SOAP `Messaging.asmx`
4. ThrottleMiddleware + ThrottleService replace legacy `Throttle.cs` — uses `IMemoryCache` for TTL expiration and `ConcurrentDictionary` + `Interlocked` for thread safety (replaces `Hashtable` + `lock(typeof(Throttle))` + `HttpContext.Current.Cache`)
5. Dapper + Microsoft.Data.SqlClient referenced but not yet invoked — ready for Sprint 2 discovery/species endpoints
6. Server registered in AppHost as Aspire resource

**Why:** Sprint 1 deliverable. The server is the first functional backend service in the modernized Terrarium stack. All three issues (#9, #10, #11) delivered in one PR.

**Impact:** Client devs can now target `/api/messaging/*` for welcome/MOTD/version instead of SOAP. Future sprints add discovery, species, and reporting endpoints.
