### Terrarium.Services is a pure class library — no ServiceDefaults dependency
**By:** Mike (Networking / Engine Dev)
**Status:** Decided
**Issue:** #14
**PR:** #109

**What:** `Terrarium.Services` depends only on `Microsoft.Extensions.Http` and `Microsoft.Extensions.Options`. It does NOT reference `Terrarium.ServiceDefaults`. This keeps it usable by any consumer (Game client, tests, CLI tools) without pulling in ASP.NET Core or OpenTelemetry.

Consumers that want Aspire integration (resilience, service discovery) configure it at the DI level:
```csharp
services.AddTerrariumServices("terrarium-server"); // Aspire service discovery
services.AddTerrariumServices(new Uri("https://api.terrarium.dev")); // explicit URI
```

**Impact:** Gus (Server Dev) — the client-side contracts are defined. Server endpoints should return JSON matching the models in `Terrarium.Services.Models`. Hank (QA) — all services are interface-based, straightforward to mock for testing.

**Why:** Interface-first with minimal dependencies is the right call. The legacy proxies were auto-generated ASMX stubs tightly coupled to SOAP. The new layer is clean, testable, and works with or without Aspire.
