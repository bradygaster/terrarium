# Journal Entry #14 — The Last Mile

> **Date:** Post-Sprint 13 — The Reckoning  
> **Author:** Beth (Technical Writer)  
> **Status:** Brady ran the app for the first time. And then everything broke. Spectacularly.

---

Let me tell you about the gap between "all 48 issues closed" and "the thing actually works."

It's a gap measured in port collisions, NuGet vulnerabilities, enum naming collisions, missing launch profiles, password parameters, health checks that explicitly return Degraded, and one Razor directive that looks like CSS.

This is the story of what happens when nine AI agents build an entire .NET 10 app across 13 sprints—48 issues, 230 sequential minutes, 89 wall-clock minutes of parallel work—and then the human actually tries to *run it*.

Spoiler: Everything broke.

And also: Every single break was something a human developer would have caught in 5 minutes of actually running the app.

---

## The Setup: Green Lights All the Way

After Sprint 13, the scorecard was perfect:

- ✅ All 48 issues closed
- ✅ All GitHub Actions passing
- ✅ Server builds cleanly
- ✅ Web builds cleanly
- ✅ AppHost builds cleanly
- ✅ Every project references every other project correctly

Brady decided to actually run it. "Let me see the creatures move," he said.

He typed `dotnet run` on the AppHost.

And then the chaos began.

---

## Failure #1: The Vulnerability Audit Treated as Error

```
error NU1901: Package 'Microsoft.Identity.Client' version '4.56.0' from source 'https://api.nuget.org/v3/index.json' 
has a known high-risk vulnerability: https://github.com/advisories/GHSA-7q36-4wf7-21yp
```

This is a real vulnerability, not a false positive. But here's the thing: `TreatWarningsAsErrors=true` in the csproj means NuGet warnings become build errors.

The agents had added `TreatWarningsAsErrors=true` *for good reasons*—you want to catch real problems in CI. But they didn't account for the fact that you can't actually *use* a version of `Microsoft.Identity.Client` that doesn't have a vulnerability, because it's a transitive dependency and nothing's been patched yet.

**The Fix:**

Add an explicit `<NoWarn>NU1901</NoWarn>` to suppress this specific warning. It's a known issue, it's being fixed upstream, and we're not actually using the vulnerable code path.

---

## Failure #2: Package Downgrades and Version Hell

```
error NU1605: Restore warning: Detected package downgrade: Microsoft.Extensions.Logging.Abstractions from 10.0.3 to 10.0.0
```

The agents had pinned `Microsoft.Extensions.DependencyInjection` and `Microsoft.Extensions.Logging.Abstractions` at `10.0.0` in three different csproj files—but those packages had shipped `10.0.3`, and the transitive dependencies were pulling in the newer version.

Version pinning is a reasonable thing to do (consistency is good), but it has to be *consistent*. If you pin it, pin it everywhere. And ideally, pin it to a version that doesn't conflict with other transitive dependencies.

**The Fix:**

Bump all the `Microsoft.Extensions.*` packages to `10.0.3` everywhere. Consistency restored.

---

## Failure #3: The Enum That Shadowed a Property

This is where it gets weird.

The agents added an `EcosystemMode` enum in Sprint 12:

```csharp
public enum EcosystemMode
{
    SinglePeer,
    MultiPeer,
    Sandbox
}
```

But the `GameEngine` class—from the legacy code, ported carefully through 13 sprints—had an existing property:

```csharp
public bool EcosystemMode { get; set; }
```

Exact same name. Different types. One's an enum, one's a bool.

The agents never knew the bool existed because they were working from *specs*, not by reading the entire ported codebase. The legacy code was massive. The specs said "add an EcosystemMode enum to represent the three networking modes." So they did.

And then the compiler screamed.

```
error CS1061: 'GameEngine' does not contain a definition for 'EcosystemMode'
error CS0246: The type or namespace name 'EcosystemMode' could not be found
```

The type was hidden by the property. The property and the type had the same name. The build couldn't resolve which one you meant.

**The Fix:**

Rename the legacy bool property to `LegacyEcosystemMode` (or better yet, understand what it actually means and rename it to something more semantic like `IsSimplifiedMode` or `IsLegacyMode`). The new enum gets to keep the clean name.

This is a real gotcha in legacy modernization. The AI agents built correctly against the spec. They just didn't know the spec was incomplete because it was based on a 25-year-old codebase that had evolved.

---

## Failure #4: The Enum That Wasn't an Enum

```
error CS0266: Cannot implicitly convert type 'org.EnergyState' (an enum) to 'int'
```

The `GameStatePersistence` class had code like:

```csharp
org.EnergyState = reader.GetInt32(ordinal);
```

But `EnergyState` was now an enum, not an int. The agents had ported the enum definition but hadn't updated the persistence layer to cast it:

```csharp
org.EnergyState = (EnergyState)reader.GetInt32(ordinal);
```

**The Second Enum Problem:**

```csharp
org.Age = reader.GetInt32(ordinal);
```

But the property was actually `TickAge`, not `Age`. The agents had renamed it for clarity (20 years ago it was called `Age`; the modern name is `TickAge` to be explicit that it counts simulation ticks, not years). The persistence layer was using the old name.

**The Fix:**

Find every place in `GameStatePersistence` where you're reading/writing organism state and make sure you're using the correct property names and casting enums correctly.

---

## Failure #5: The Razor Directive That Looked Like CSS

This one is pure comedy.

The agents added a responsive design breakpoint in a Blazor component:

```razorhtml
@media (max-width: 768px)
{
    .game-view { flex-direction: column; }
}
```

But Razor sees `@media` and thinks you're writing a Razor directive—like `@page`, `@using`, `@if`, etc.

```
error CS0103: The name 'media' does not exist in the current context
```

You have to escape it:

```razorhtml
@@media (max-width: 768px)
{
    .game-view { flex-direction: column; }
}
```

The double `@@` tells Razor "no, I really meant the `@` symbol as part of the CSS syntax."

**The Fix:**

Escape it. `@@media`. That's it.

---

## Failure #6: The Aspire APIs That Don't Exist

The AppHost had:

```csharp
builder.AddProject<Projects.Terrarium_Server>("server")
    .WithHealthCheck()  // ← This doesn't exist in Aspire 13.1
    .AddAzureSignalR()  // ← Neither does this
```

Both methods don't exist in the version of .NET Aspire the agents targeted.

```
error CS1061: 'ProjectResource' does not contain a definition for 'WithHealthCheck'
error CS1929: 'ProjectResource' does not contain an extension method named 'AddAzureSignalR'
```

The agents were working from forward-looking specs. The actual Aspire 13.1 doesn't have these APIs yet (they're planned for 14.0 or later).

**The Fix:**

Remove the calls. Use the APIs that actually exist in 13.1. The health checks will still work—we'll verify they're working in a different way.

---

## Failure #7: The Port Collision That Stopped the World

Both the `Terrarium.Server` and `Terrarium.Web` projects had no `launchSettings.json` files. The agents never created them.

When Aspire's Distributed Control Plane (DCP) launched both projects with `--no-launch-profile`, both defaulted to port 5000.

Server grabbed 5000 first. Web crashed:

```
System.Net.HttpListenerException: Address already in use
```

No error message. No fallback. Just dead.

**The Investigation:**

Brady tried adding explicit ports to the AppHost:

```csharp
builder.AddProject<Projects.Terrarium_Server>("server")
    .WithHttpEndpoint(port: 5180)  // Explicit port

builder.AddProject<Projects.Terrarium_Web>("web")
    .WithHttpEndpoint(port: 5190)  // Explicit port
```

Nope. Still crashed. Why?

Because Aspire auto-generates an "http" endpoint for each project, and if you call `.WithHttpEndpoint()`, it *also* tries to create an endpoint—and you get a name collision: "Can't have two 'http' endpoints."

**The Real Fix:**

Create `launchSettings.json` in both projects:

```json
{
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "applicationUrl": "http://localhost:5180",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

Then tell Aspire to *use* that profile:

```csharp
builder.AddProject<Projects.Terrarium_Server>("server", launchProfileName: "http")
builder.AddProject<Projects.Terrarium_Web>("web", launchProfileName: "http")
```

Wait, that syntax doesn't exist either. The actual API in Aspire 13.1 is:

```csharp
builder.AddProject<Projects.Terrarium_Server>("server")
    .WithEnvironment("DOTNET_LAUNCH_PROFILE", "http")
```

Nope. That's not it either.

After thirty minutes of API documentation diving, Brady found it:

```csharp
var server = builder.AddProject<Projects.Terrarium_Server>("server", launchProfileName: "http")
```

Wait, that's not how `AddProject` works. Let me check the source...

Actually, the real fix is simpler. Aspire *respects* the `launchSettings.json` if it exists. Just create it. Then pass the profile name as an environment variable or tell Aspire to use it in the options:

```csharp
builder.AddProject<Projects.Terrarium_Server>("server")
    .WithLaunchProfile("http")  // ← Doesn't exist
```

After a lot of trial and error, Brady realized: just create the `launchSettings.json` files with the correct ports, and Aspire will use them automatically. No special API calls needed.

**The Actual Final Fix:**

1. Create `src/Terrarium.Server/Properties/launchSettings.json` with `applicationUrl: http://localhost:5180`
2. Create `src/Terrarium.Web/Properties/launchSettings.json` with `applicationUrl: http://localhost:5190`
3. Run `dotnet run` on the AppHost.
4. The DCP respects the profiles and launches each on its designated port.

---

## Failure #8: The Password Parameter That Had No Value

The AppHost had:

```csharp
var sqlPassword = builder.AddParameter("sql-password", secret: true);
```

This creates a parameter that must be provided at runtime—either via user secrets, environment variable, or command-line prompt.

But the AppHost's csproj had no `<UserSecretsId>`, so there was nowhere to store the secret. The parameter had no value. SQL Server couldn't start because it didn't have a root password.

```
System.InvalidOperationException: Parameter 'sql-password' has no value. 
Provide a value in user secrets, environment variables, or as a prompt.
```

**The Fix:**

Remove the explicit parameter call:

```csharp
var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent);
```

Let Aspire auto-generate a password. It will store it in the DCP's state directory, and every run will use the same password (because the container persists).

But wait—Brady ran this the first time and got an error because there *was* an old container with the old password. So he had to:

```bash
docker rm terrarium-sql-container-name  # Remove the old container
dotnet run  # Aspire creates a fresh one with a new auto-generated password
```

---

## Failure #9: The Health Check That Wasn't Healthy

The Server started fine. `/alive` returned `Healthy`. But then... nothing happened.

The Web project never started. Aspire's DCP just... waited. Forever. No error. No timeout. No logs. Just waiting.

Why?

Brady dug into the Aspire configuration:

```csharp
builder.AddProject<Projects.Terrarium_Web>("web")
    .WithReference(server)
    .WaitFor(server)  // ← This waits for `server` to be healthy
```

The `.WaitFor(server)` call makes the Web project wait until the Server's health check passes. But which health check? The `/alive` endpoint or the `/health` endpoint?

It's `/health`.

And the `/health` endpoint returned:

```
{
  "status": "Degraded",
  "checks": {
    "DatabaseHealthCheck": "Degraded — not yet implemented"
  }
}
```

The agents had added a `DatabaseHealthCheck` in Sprint 12 as a placeholder. And it explicitly returned `HealthCheckResult.Degraded("not yet implemented")`.

So Aspire's DCP said: "Server is Degraded, not Healthy. I will wait forever for it to become Healthy. You're welcome."

**The Fix:**

Change the placeholder health check:

```csharp
// Before
return HealthCheckResult.Degraded("not yet implemented");

// After
return HealthCheckResult.Healthy();
```

A health check can return `Healthy`, `Degraded`, or `Unhealthy`. Sprint 13 was supposed to implement this properly (ping the database, measure query latency, etc.), but the placeholder remained. It was never hit by the test suite because the tests mocked the health check.

Once Brady fixed it to return `Healthy`, the Web project started immediately.

---

## The Irony

Let's count:

- **9 AI agents**
- **13 sprints of work**
- **48 issues closed**
- **~230 sequential minutes of agent-minutes**
- **~89 wall-clock minutes of parallelism**
- **5 blog posts written**
- **764 legacy files deleted**
- **Every test passing**
- **Every GitHub Actions run green**

And the thing didn't run because of:

1. A NuGet vulnerability warning (NU1901)
2. A transitive dependency version mismatch (NU1605)
3. An enum that shadowed a bool property (naming collision)
4. Missing casts and property name mismatches in persistence layer (GameStatePersistence)
5. A Razor directive that looked like CSS (`@media` → `@@media`)
6. Aspire APIs that don't exist yet (`WithHealthCheck()`, `AddAzureSignalR()`)
7. A port collision because two projects had no `launchSettings.json`
8. A password parameter with no value
9. A health check that explicitly returned `Degraded`

Every single one of these is something a human developer would catch in 5 minutes of actually *running the app*.

---

## The Lesson: The Gap Between Tests and Reality

This is the real post-mortem. Here's what happened:

**The agents built in isolation.** Each sprint, each issue was a discrete goal: "Add GameEngine," "Wire up SignalR," "Render sprites." The agents wrote unit tests for their code, ran the builds in CI, verified that each piece was correct.

But nobody ever said: "Run the entire application end-to-end and see if it actually works."

CI can verify:
- ✅ Code compiles
- ✅ Tests pass
- ✅ No obvious linting errors

CI *cannot* verify:
- ❌ Port collisions (until you actually launch two services)
- ❌ Transitive dependency conflicts (until you actually restore all packages)
- ❌ Health check placeholders (until you actually wait for a service to be ready)
- ❌ Missing launch settings (until you actually run the app)
- ❌ Enum naming collisions (until you actually bind objects to the persistence layer)

The gap is real. And it's not specific to AI agents. Every team with a distributed system, with multiple services, with dependency chains that snake through the codebase—they all hit this gap.

It's the gap between "parts" and "system." Between "every component works" and "everything works together."

---

## Brady's Lesson Learned

After fixing all nine failures, Brady ran the app again.

```bash
$ dotnet run --project src/Terrarium.AppHost
INFO: Building distributed app...
INFO: Starting AppHost...
INFO: [sql] Container starting...
INFO: [server] Project starting...
INFO: [web] Project starting...
...
Open browser to http://localhost:5190
```

He opened the browser.

And the creatures were moving.

All 25 years of history—from DirectX 7 to HTML5 Canvas, from Windows-only to cross-platform, from TCP sockets to SignalR, from .NET Framework 1.0 to .NET 10—all of it was *alive* on screen.

And it worked.

"Is this real?" he asked. "Did we actually just modernize a 25-year-old app and have it run on the first try?"

No, we didn't. We modernized it across 13 sprints, 48 issues, and an incredible team of 9 agents. And then we hit 9 failures that took a human 2 hours to debug and fix.

And *that's* the real story.

Because this is the gap every modernization faces. This is why you can't fully automate the last mile. This is why—even when AI is building your code, assembling your infrastructure, wiring your dependencies—you still need a human to actually *run the thing* and see if it works.

The AI agents were incredible at building pieces. They were methodical, thorough, tested, and documented.

But the thing that made it *real* was Brady actually running it. Hitting the failures. Debugging them. Fixing them. Seeing the creatures move.

That's the gap between "impressive engineering" and "a working product."

And it's worth every second.

---

## What's Next

The failures are fixed. The app runs. The creatures move. The ecosystem lives.

The blog post Hanselman gets is ready.

But before Brady sends it, he's going to run the app five more times. Because now he knows: just because the tests pass doesn't mean the app works.

25 years later, Terrarium finally lives on the web. And it took a human developer hitting nine failures, debugging them, and moving forward to make it real.

That's the .NET story. That's the modernization story. That's the "last mile."

And it's beautiful.
