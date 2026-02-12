# Error Handling Architecture

## Overview

Comprehensive error handling and resilience across the Terrarium stack, implemented in Sprint 12.

## Components

### 1. Global Exception Handler — TerrariumErrorBoundary

**Location:** `src/Terrarium.Web/Components/Shared/TerrariumErrorBoundary.razor`

**Purpose:** Blazor ErrorBoundary component that wraps major page sections to catch and display rendering exceptions gracefully.

**Features:**
- Catches unhandled exceptions in the component tree
- Shows user-friendly error message with optional details
- Provides "Try Again" recovery action
- Optional fallback route navigation
- Logs all exceptions via ILogger
- Supports custom error callbacks for telemetry

**Usage:**

```razor
<TerrariumErrorBoundary ShowDetails="@IsDevelopment" FallbackRoute="/">
    @Body
</TerrariumErrorBoundary>
```

**Parameters:**
- `ShowDetails` (bool): Show exception message and stack trace. Set to `false` in production.
- `FallbackRoute` (string?): Optional route to navigate to (e.g., "/")
- `FallbackLabel` (string): Label for fallback button (default: "Home")
- `OnError` (EventCallback<Exception>): Callback for custom error handling/telemetry

**Integrated into:**
- `MainLayout.razor` — wraps all page content
- `Routes.razor` — custom 404 page

---

### 2. Graceful Degradation — Local-Only Mode

**Location:** `src/Terrarium.Web/Components/Pages/Home.razor`

**Purpose:** When the server is unreachable, the game automatically switches to local-only mode, allowing the ecosystem to continue running without network features.

**Behavior:**
- Monitors SignalR connection lifecycle events (`OnClosed`, `OnReconnecting`, `OnReconnected`)
- Tracks connection attempts (max 3 before switching to local-only)
- Disables network-dependent features (peer list, teleportation, population reporting)
- Shows user-friendly messages in the event log
- Automatically exits local-only mode when reconnection succeeds

**Key Fields:**
```csharp
private bool _localOnlyMode = false;
private int _connectionAttempts = 0;
private const int MaxConnectionAttempts = 3;
```

**Key Methods:**
- `HandleConnectionClosed(Exception?)` — increments attempt counter
- `HandleReconnecting(string?)` — shows reconnecting message
- `HandleReconnected(string?)` — resets counters, exits local-only mode
- `SwitchToLocalOnlyMode(string reason)` — disables network features, notifies user

---

### 3. SignalR Reconnection with Exponential Backoff

**Location:** `src/Terrarium.Web/Services/TerrariumHubClient.cs`

**Purpose:** Automatically reconnects when the SignalR connection drops, using exponential backoff.

**Policy:** `ExponentialBackoffRetryPolicy`
- Attempt 1: immediate
- Attempt 2: 2 seconds
- Attempt 3: 10 seconds
- Attempt 4: 30 seconds
- Attempt 5: 60 seconds
- After 5 attempts: give up

**Already Implemented:** This was already present in the codebase. Sprint 12 confirms it matches architecture spec.

**Events:**
- `OnReconnecting` — fired when connection drops and retry begins
- `OnReconnected` — fired when connection restores
- `OnClosed` — fired when connection closes permanently

---

### 4. Retry Logic for HTTP Calls

**Location:** `src/Terrarium.Services/ServiceCollectionExtensions.cs`

**Purpose:** All HTTP service clients (PopulationService, SpeciesService, etc.) automatically retry transient failures using the standard resilience handler.

**Implementation:**

Each `AddHttpClient<TInterface, TImplementation>()` call now includes:

```csharp
.AddStandardResilienceHandler();
```

This is provided by `Microsoft.Extensions.Http.Resilience` and configures:
- **Retry policy:** 3 attempts with exponential backoff
- **Circuit breaker:** Stops requests after repeated failures, then retries after timeout
- **Timeout:** Per-request timeout with cancellation

**Applies to all service clients:**
- IMessagingService
- IPeerDiscoveryService
- ISpeciesService
- IPopulationService
- IReportingService
- IChartService
- IUsageService
- IWatsonService

**Already in ServiceDefaults:** The `ConfigureHttpClientDefaults()` in `Extensions.cs` already adds `.AddStandardResilienceHandler()` globally. Sprint 12 explicitly adds it to each service client for clarity and control.

---

### 5. Enhanced Error Handling in Service Calls

**Location:**
- `src/Terrarium.Web/Components/Pages/Upload.razor`
- `src/Terrarium.Web/Components/Pages/Gallery.razor`
- `src/Terrarium.Game/Services/GameServiceBridge.cs`

**Purpose:** Distinguish between different error types and handle them appropriately.

**Pattern:**

```csharp
try
{
    // Service call
}
catch (HttpRequestException ex)
{
    // Network error — server unreachable
    Logger.LogError(ex, "Network error...");
}
catch (TaskCanceledException ex)
{
    // Request timeout
    Logger.LogError(ex, "Request timeout...");
}
catch (InvalidOperationException ex)
{
    // Validation or business logic error
    Logger.LogError(ex, "Validation error...");
}
catch (Exception ex)
{
    // Unexpected error
    Logger.LogError(ex, "Unexpected error...");
}
```

**Already in GameServiceBridge:** All service calls in `GameServiceBridge.cs` already catch exceptions and log warnings. No changes needed — pattern confirmed correct.

---

## Error Flow Diagram

```
User Action
    |
    v
Component Layer (Blazor)
    |
    +--[Render Error]---> TerrariumErrorBoundary
    |                         |
    |                         +---> Show error UI
    |                         +---> Log exception
    |                         +---> Optionally navigate to fallback
    |
    +--[Service Call]---> HttpClient with Resilience Handler
    |                         |
    |                         +--[Transient Failure]---> Retry (up to 3x)
    |                         +--[Repeated Failures]---> Circuit Breaker Opens
    |                         +--[Timeout]--------------> TaskCanceledException
    |                         +--[Network Error]--------> HttpRequestException
    |                         +--[Success]--------------> Continue
    |
    +--[SignalR Call]---> TerrariumHubClient
                              |
                              +--[Connection Drops]-----> Exponential Backoff Reconnect
                              +--[Max Retries Failed]---> OnClosed event
                              |                              |
                              |                              v
                              |                        Home.HandleConnectionClosed()
                              |                              |
                              |                              +--[Attempts >= 3]---> SwitchToLocalOnlyMode()
                              |
                              +--[Reconnected]----------> OnReconnected event
                                                              |
                                                              v
                                                        Home.HandleReconnected()
                                                              |
                                                              +---> Exit local-only mode
```

---

## Testing Scenarios

### 1. SignalR Connection Failure
**Test:** Stop the server while the client is running.
**Expected:**
- SignalR reconnection attempts appear in logs
- After 3 failed attempts, Home page shows "Running in local-only mode"
- Peer count drops to 0
- Game continues running locally

### 2. HTTP Service Failure
**Test:** Introduce network delay or stop server during species registration.
**Expected:**
- HTTP client retries up to 3 times with exponential backoff
- If all retries fail, exception is caught and logged
- User sees appropriate error message (e.g., "Network error — please try again")

### 3. Component Render Error
**Test:** Introduce a null reference exception in a component.
**Expected:**
- TerrariumErrorBoundary catches the exception
- User sees error UI with "Try Again" button
- Exception logged to console
- Rest of application continues working

### 4. File Upload Error
**Test:** Upload an invalid DLL or simulate disk write failure.
**Expected:**
- Validation errors shown in Upload page UI
- Temp file cleaned up automatically
- Detailed error logged
- User can "Try Again"

---

## Configuration

### Development vs Production

**Development:**
- `TerrariumErrorBoundary.ShowDetails = true` — shows stack traces
- Detailed exception messages in UI
- All errors logged at Error level

**Production:**
- `TerrariumErrorBoundary.ShowDetails = false` — generic error message
- Stack traces hidden from users
- Errors logged to telemetry (OpenTelemetry)

**Set in:** `MainLayout.razor`

```razor
<TerrariumErrorBoundary ShowDetails="@IsDevelopment">
    @Body
</TerrariumErrorBoundary>

@code {
    [Inject]
    private IWebHostEnvironment Environment { get; set; } = default!;

    private bool IsDevelopment => Environment.IsDevelopment();
}
```

---

## Future Enhancements

1. **Toast notifications** — show transient errors as dismissible toasts instead of inline messages
2. **Telemetry integration** — send all caught exceptions to Application Insights or OpenTelemetry
3. **Offline queue** — buffer network operations when in local-only mode, replay when reconnected
4. **Health checks** — proactive server health monitoring before attempting operations
5. **User-facing retry** — "Retry" buttons for failed operations (e.g., species introduction)

---

## Related Files

- `src/Terrarium.Web/Components/Shared/TerrariumErrorBoundary.razor`
- `src/Terrarium.Web/Components/Shared/TerrariumErrorBoundary.razor.css`
- `src/Terrarium.Web/Components/Layout/MainLayout.razor`
- `src/Terrarium.Web/Components/Routes.razor`
- `src/Terrarium.Web/Components/Pages/Home.razor`
- `src/Terrarium.Web/Components/Pages/Upload.razor`
- `src/Terrarium.Web/Components/Pages/Gallery.razor`
- `src/Terrarium.Web/Services/TerrariumHubClient.cs`
- `src/Terrarium.Services/ServiceCollectionExtensions.cs`
- `src/Terrarium.ServiceDefaults/Extensions.cs`
- `src/Terrarium.Game/Services/GameServiceBridge.cs`
- `src/Terrarium.Game/Networking/NetworkEngine.cs`

---

## Sprint 12 Checklist

- [x] Global exception handler — Blazor error boundary components wrapping major page sections
- [x] Graceful degradation when server is unreachable — game runs in local-only mode
- [x] SignalR reconnection with exponential backoff (confirmed already in TerrariumHubClient)
- [x] Retry logic for network calls (HttpClient calls to server endpoints)
- [x] Blazor ErrorBoundary component — created `TerrariumErrorBoundary.razor`
- [x] Integration into MainLayout and Routes
- [x] Enhanced error handling in Upload and Gallery pages
- [x] Documentation (this file)

---

## Build Command

```bash
dotnet build src/Terrarium.sln
```

All changes respect `TreatWarningsAsErrors=true` and target `net10.0`.
