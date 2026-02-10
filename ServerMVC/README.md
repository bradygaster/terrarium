# Terrarium Server — Architecture & Domain Reference

> Written by Gus, Server Dev. Last updated during initial domain exploration.

## Overview

The Terrarium server exists in **two forms**: a legacy ASP.NET WebForms/ASMX web service application (`Server/Website/`) and a modern ASP.NET MVC 2 application (`ServerMVC/TerrariumServer/`). The legacy server is the real workhorse — it contains all the game logic, web service endpoints, database access, and ecosystem management. The MVC server is essentially a scaffold: a fresh MVC 2 project template with account management only. No game logic has been ported to it yet.

## MVC Server (`ServerMVC/TerrariumServer/`)

### Architecture

- **Framework:** ASP.NET MVC 2, targeting .NET Framework 4.0
- **Solution:** `TerrariumServer.sln`
- **Routing:** Default `{controller}/{action}/{id}` convention, registered in `Global.asax.cs`

### Controllers

| Controller | File | Purpose |
|---|---|---|
| `HomeController` | `Controllers/HomeController.cs` | Two actions: `Index` (welcome page) and `About`. Template-level only. |
| `AccountController` | `Controllers/AccountController.cs` | Full account lifecycle: `LogOn`, `LogOff`, `Register`, `ChangePassword`. Uses `IFormsAuthenticationService` and `IMembershipService` interfaces for testability. |

### Models

| Model | File | Purpose |
|---|---|---|
| `LogOnModel` | `Models/AccountModels.cs` | Username, password, remember-me flag |
| `RegisterModel` | `Models/AccountModels.cs` | Username, email, password, confirm-password with `[PropertiesMustMatch]` validation |
| `ChangePasswordModel` | `Models/AccountModels.cs` | Old password, new password, confirm with validation |
| `IMembershipService` | `Models/AccountModels.cs` | Interface wrapping `MembershipProvider` for DI/testing |
| `IFormsAuthenticationService` | `Models/AccountModels.cs` | Interface wrapping `FormsAuthentication` static methods |

### Views

Standard MVC 2 WebForms view engine (`.aspx`):

- `Views/Home/` — `Index.aspx`, `About.aspx`
- `Views/Account/` — `LogOn.aspx`, `Register.aspx`, `ChangePassword.aspx`, `ChangePasswordSuccess.aspx`
- `Views/Shared/` — `Site.Master` (layout), `Error.aspx`, `LogOnUserControl.ascx`

### What's NOT Here

The MVC project has **zero game-related logic**. No creature registration, no peer discovery, no population reporting, no species management. It's a starting point that was never completed.

---

## Test Project (`ServerMVC/TerrariumServer.Tests/`)

### Framework

- MSTest (`Microsoft.VisualStudio.TestTools.UnitTesting`)

### Coverage

| Test Class | File | What's Tested |
|---|---|---|
| `HomeControllerTest` | `Controllers/HomeControllerTest.cs` | `Index` returns correct ViewData message; `About` returns a non-null ViewResult |
| `AccountControllerTest` | `Controllers/AccountControllerTest.cs` | Full coverage of `LogOn`, `LogOff`, `Register`, `ChangePassword` — success paths, failure paths, ModelState validation, redirects |

The `AccountControllerTest` is well-structured. It uses mock implementations of `IFormsAuthenticationService` and `IMembershipService` — proper constructor injection via public setters. The mock membership service has hardcoded test data (`"someUser"`, `"goodPassword"`, `"duplicateUser"` etc.).

**Test count:** 15 test methods total (2 Home + 13 Account).

---

## Legacy Server (`Server/`)

### Architecture

Classic ASP.NET 2.0 WebForms site with ASMX web services. All code lives in `Website/App_Code/`. No compiled project — this is a "Web Site" deployment model, not a "Web Application."

### Web Service Endpoints (ASMX)

These are the real API contracts that clients depend on:

| Service | URL Path | Class | Purpose |
|---|---|---|---|
| **Peer Discovery** | `/Discovery/discoverydb.asmx` | `PeerDiscoveryService` | Peer registration, counting, validation, version checking |
| **Species** | `/Species/addspecies.asmx` | `SpeciesService` | Creature upload, retrieval, reintroduction, blacklist checking |
| **Reporting** | `/Reporting/reportpopulation.asmx` | `ReportingService` | Population data collection from clients |
| **Messaging** | `/Messaging/messaging.asmx` | `Messaging` | Welcome messages, MOTD, latest version info |
| **Watson** | `/Watson/watson.asmx` | `WatsonService` | Client error/crash reporting |
| **Bug Reporting** | `/BugReporting/BugService.asmx` | `BugService` | Bug submission (stub — `TODO: Add code`) |
| **Charts** | `/Charts/chartservice.asmx` | `ChartService` | Chart data generation for statistics display |
| **Usage** | `/Reporting/UsageService.asmx` | `UsageService` | Usage statistics reporting |

### API Contracts (Detail)

#### PeerDiscoveryService

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `RegisterUser` | `email` | `bool` | Registers a client user by email + IP via `TerrariumRegisterUser` sproc |
| `GetNumPeers` | `version`, `channel` | `int` | Peer count for a version/channel via `TerrariumGrabNumPeers` sproc |
| `ValidatePeer` | *(none)* | `string` (IP) | Returns the caller's IP address as validation |
| `RegisterMyPeerGetCountAndPeerList` | `version`, `channel`, `guid` | `RegisterPeerResult` + out `DataSet peers`, out `int count` | The big one — registers peer, returns peer list + count. Uses `TerrariumRegisterPeerCountAndList` sproc. Returns `GlobalFailure` if version is disabled. |
| `IsVersionDisabled` | `version` | `bool` + out `string errorMessage` | Admin kill-switch per version via `TerrariumIsVersionDisabled` sproc |

#### SpeciesService

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `Add` | `name`, `version`, `type`, `author`, `email`, `assemblyFullName`, `assemblyCode` (byte[]) | `SpeciesServiceStatus` | Upload a new creature. Checks word filter, enforces 5-min and 24-hr throttle, saves assembly DLL to disk. |
| `GetExtinctSpecies` | `version`, `filter` | `DataSet` | List species with zero population (available for reintroduction) |
| `GetAllSpecies` | `version`, `filter` | `DataSet` | List all species for a version. Checks if version is disabled. |
| `GetSpeciesAssembly` | `name`, `version` | `byte[]` | Download a creature's compiled assembly |
| `ReintroduceSpecies` | `name`, `version`, `peerGuid` | `byte[]` | Mark extinct species as alive + return its assembly |
| `GetBlacklistedSpecies` | *(none)* | `string[]` | Get assembly names of all blacklisted species |

#### ReportingService

| Method | Parameters | Returns | Description |
|---|---|---|---|
| `ReportPopulation` | `DataSet data`, `Guid guid`, `int currentTick` | `int` (ReturnCode) | Accepts population data per species per tick. Validates against blacklist, checks for node timeout/corruption, enforces throttling (3-min + 12-hr), validates data bounds (max 600 species rows, max 340 population per species, max 600 total). Inserts into `History` table. |

#### Messaging

| Method | Returns | Description |
|---|---|---|
| `GetWelcomeMessage` | `string` | From `ServerSettings.WelcomeMessage` |
| `GetMessageOfTheDay` | `string` | From `ServerSettings.MOTD` |
| `GetLatestVersion` | `string` | From `ServerSettings.LatestVersion` |

#### WatsonService

| Method | Parameters | Description |
|---|---|---|
| `ReportError` | `DataSet data` | Inserts client crash logs into the `Watson` table |

### Supporting Infrastructure

| File | Purpose |
|---|---|
| `App_Code/Code/ServerSettings.cs` | Centralized configuration (connection strings, paths, throttle limits, MOTD). All settings from `appSettings` in `web.config`. |
| `App_Code/Code/Throttle.cs` | In-memory rate limiter using ASP.NET Cache for expiry. Per-IP tracking with configurable max-count and TTL. |
| `App_Code/Code/NonPageServices.cs` | Singleton background timer that runs `TerrariumAggregate` stored procedure on interval (default 10 min) to roll up reporting data + refresh chart species list. |
| `App_Code/Code/ChartBuilder.cs` | Generates chart data for species population graphs |
| `App_Code/Code/WordFilter.cs` | Content moderation — checks species names, author names, emails against `invalidwordlist.txt` |
| `App_Code/Code/ErrorLog.cs` | Error logging utilities |
| `App_Code/Code/Installer.cs` | Performance counter and event log setup |
| `App_Code/UsageReporting.cs` | Usage analytics — per-user and per-team hours, with caching |
| `App_Code/UsageData.cs` | Data model for usage records |
| `App_Code/BugService.cs` | Bug reporting — **stub only**, `TODO` in the implementation |

---

## Database Schema (`Server/1_CreateDatabase.sql`, `Server/2_CreateDatabaseTables.sql`)

Database name: **TerrariumWhidbey** (SQL Server, compat level 80).

### Tables

| Table | Purpose | Key Columns |
|---|---|---|
| `Watson` | Client crash/error reports | `id` (identity), `LogType`, `MachineName`, `OSVersion`, `GameVersion`, `CLRVersion`, `ErrorLog`, `UserEmail`, `UserComment`, `DateSubmitted` |
| `UsageSummary` | Periodic peer count snapshots | `Peers`, `SummaryDateTime` |
| `Usage` | Per-client usage telemetry | `Alias`, `Domain`, `TickTime`, `UsageMinutes`, `IPAddress`, `GameVersion`, `PeerChannel`, `PeerCount`, `AnimalCount`, `MaxAnimalCount`, `WorldHeight`, `WorldWidth`, `MachineName`, `OSVersion`, `ProcessorCount`, `ClrVersion`, `WorkingSet`, `ProcessorTime`, `ProcessStartTime` |
| `PumTeam` | Team membership | `PumId`, `Alias`, `ManagerAlias` |
| `Pum` | Team leads/managers | `Id` (identity), `Name`, `Alias`, `TeamName` |
| `DailyPopulation` | Rolled-up species population data | `SampleDateTime`, `SpeciesName`, `Population`, `BirthCount`, `StarvedCount`, `KilledCount`, `ErrorCount`, `TimeoutCount`, `SickCount`, `OldAgeCount`, `SecurityViolationCount` |
| `Downloads` | Download tracking | `Filename`, `DownloadCount`, `LastDownloadDate` |
| `History` | Raw per-tick population reports from clients | `GUID`, `TickNumber`, `SpeciesName`, `ContactTime`, `ClientTime`, `CorrectTime`, `Population`, `BirthCount`, `TeleportedToCount`, `StarvedCount`, `KilledCount`, `TeleportedFromCount`, `ErrorCount`, `TimeoutCount`, `SickCount`, `OldAgeCount`, `SecurityViolationCount` |
| `NodeLastContact` | Tracks last heartbeat per peer | `GUID`, `LastTick`, `LastContact` |
| `Peers` | Active peer registrations | `Channel`, `IPAddress`, `Lease`, `Version`, `Guid`, `FirstContact` |
| `RandomTips` | Tips shown to users | `id` (identity), `tip` |
| `ShutdownPeers` | Peers that have disconnected | `Guid`, `Channel`, `IPAddress`, `Version`, `FirstContact`, `LastContact`, `UnRegister` |
| `Species` | Registered creature species | `Name`, `Type`, `Author`, `AuthorEmail`, `DateAdded`, `AssemblyFullName`, `Extinct`, `LastReintroduction`, `ReintroductionNode`, `Version`, `BlackListed`, `ExtinctDate` |
| `TimedOutNodes` | Nodes that missed their check-in window | `GUID`, `TimeoutDate` |
| `UserRegister` | User registrations | `IPAddress`, `Email` |
| `VersionedSettings` | Per-version admin controls | `Version`, `Disabled`, `Message` |

### Key Stored Procedures

Referenced in code (defined in the large SQL script):

- `TerrariumRegisterUser` — User registration
- `TerrariumGrabNumPeers` — Peer count query
- `TerrariumRegisterPeerCountAndList` — Combined peer register + list
- `TerrariumIsVersionDisabled` — Version kill-switch check
- `TerrariumInsertSpecies` — Species insertion
- `TerrariumCheckSpeciesExtinct` — Extinction status check
- `TerrariumReintroduceSpecies` — Mark species as alive
- `TerrariumGrabExtinctSpecies` / `TerrariumGrabExtinctRecentSpecies` — Extinct species queries
- `TerrariumGrabAllSpecies` / `TerrariumGrabAllRecentSpecies` — All species queries
- `TerrariumCheckSpeciesBlacklist` — Blacklist check
- `TerrariumInsertHistory` — Population data insertion
- `TerrariumTimeoutReport` — Node timeout/corruption detection
- `TerrariumAggregate` — Data rollup (expiration, rollup, timeout add/delete, extinction)
- `TerrariumInsertWatson` — Error log insertion
- `TerrariumReportUsageSummary` — Peer count snapshot
- `TerrariumReportUsage` — Usage data insertion
- `Web_GetTips` — Random tips query

---

## Legacy vs. Modern Server Comparison

| Aspect | Legacy (`Server/Website/`) | MVC (`ServerMVC/TerrariumServer/`) |
|---|---|---|
| **Framework** | ASP.NET 2.0 Web Site + ASMX | ASP.NET MVC 2 (.NET 4.0) |
| **Game Logic** | Full implementation | None |
| **API Endpoints** | 7+ ASMX web services | 0 game APIs |
| **Data Access** | Direct ADO.NET + stored procedures | None |
| **Authentication** | IP-based tracking | ASP.NET Membership |
| **Test Coverage** | None | 15 MSTest methods (account/home only) |
| **Content Moderation** | Word filter + blacklisting | None |
| **Rate Limiting** | In-memory throttle system | None |
| **Background Processing** | Timer-based data rollup | None |
| **Status** | Feature-complete but dated | Scaffold only |

## How the Server Supports the Ecosystem

The server is the **central nervous system** of Terrarium. Here's the flow:

1. **Peer Discovery** — Clients call `RegisterMyPeerGetCountAndPeerList` on startup and periodically. The server tracks peers by IP/channel/version, returns a peer list for P2P connections, and enforces version compatibility.

2. **Creature Registration** — Developers upload compiled creature assemblies via `SpeciesService.Add`. The server stores metadata in `Species` table and saves the DLL to disk under `{AssemblyPath}/{version}/{name}.dll`.

3. **Population Reporting** — Each client periodically calls `ReportPopulation` with per-species stats (births, deaths, starvation, kills, etc.). The server inserts raw data into `History`, then a background timer aggregates it into `DailyPopulation`.

4. **Reintroduction** — When a species goes extinct (population = 0 across all peers), it becomes available for reintroduction. A client can call `ReintroduceSpecies` to bring it back.

5. **Statistics & Charts** — The `NonPageServices` rollup + `ChartBuilder` generate visual data about the ecosystem's health over time.

6. **Safety** — Word filter on all user-submitted strings; blacklisting for misbehaving species; throttling to prevent abuse (5-min between uploads, 30/day limit, 3-min between reports).

---

## Key Files Quick Reference

| File | What It Does |
|---|---|
| `ServerMVC/TerrariumServer/Controllers/HomeController.cs` | MVC home page (template) |
| `ServerMVC/TerrariumServer/Controllers/AccountController.cs` | MVC account management |
| `ServerMVC/TerrariumServer/Models/AccountModels.cs` | Auth models + service interfaces |
| `ServerMVC/TerrariumServer/Global.asax.cs` | MVC route registration |
| `Server/Website/App_Code/Discovery/DiscoveryDB.asmx.cs` | Peer discovery service (the critical one) |
| `Server/Website/App_Code/Species/AddSpecies.asmx.cs` | Species upload/retrieval/reintroduction |
| `Server/Website/App_Code/Reporting/ReportPopulation.asmx.cs` | Population data collection |
| `Server/Website/App_Code/Messaging/Messaging.asmx.cs` | MOTD + version info |
| `Server/Website/App_Code/Watson/Watson.asmx.cs` | Crash reporting |
| `Server/Website/App_Code/Code/ServerSettings.cs` | All configuration |
| `Server/Website/App_Code/Code/Throttle.cs` | Rate limiting |
| `Server/Website/App_Code/Code/NonPageServices.cs` | Background data aggregation |
| `Server/1_CreateDatabase.sql` | Database creation script |
| `Server/2_CreateDatabaseTables.sql` | Tables, stored procedures, seed data |
