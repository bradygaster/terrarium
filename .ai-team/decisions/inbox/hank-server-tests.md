### 2026-02-11: Terrarium.Server.Tests created with integration test suite
**By:** Hank (Tester/QA)
**Status:** Implemented
**Issue:** #13

**What:** Created `src/Terrarium.Server.Tests/` — xUnit integration test project using `WebApplicationFactory<Program>` pattern. 17 tests across 3 test classes:
- **ServerHealthTests** — server boot, root endpoint, /health, /alive
- **MessagingEndpointTests** — GET /api/messaging/welcome, /motd, /version with JSON response validation
- **ThrottleTests** — rate limiting: single request success, burst success, 429 enforcement, Retry-After header, body message

4 tests pass now (bare scaffold), 13 await Gus's server bootstrap. Added `public partial class Program { }` to enable `WebApplicationFactory<Program>` access. Project added to `src/Terrarium.sln`.

**Why:** Tests are written against expected behavior derived from legacy code analysis (Messaging.asmx.cs, Throttle.cs, ServerSettings.cs). When Gus's implementation lands, these tests will validate the server endpoints immediately. Tests should not need major adjustment — the JSON shapes and endpoint paths match what was seen on squad/9-server-bootstrap.
