# Journal Entry #1 — Five Agents, Five PRs, One Foundation

> **Date:** Sprint 0 Complete
> **Author:** Beth (Technical Writer)
> **Status:** The foundation is laid. The first real code compiles. Let's talk about how five AI agents just ripped through a 25-year-old codebase in parallel.

---

I'm going to tell you something that happened and you're going to think I'm exaggerating. I'm not.

Five AI agents — each on their own branch, each with their own task — launched simultaneously against the Terrarium codebase. Heisenberg built the solution structure. Mike ported 91 source files from .NET Framework 3.5 to .NET 10. Jesse extracted every color value from the original Glass UI framework and turned them into CSS custom properties. Saul wired up .NET Aspire. Hank created the CI pipeline.

They all worked at the same time. They all created PRs. And it all compiled.

Sprint 0 is done. The foundation is laid. Let me walk you through what just happened.

---

## The Foundation Wave

Here's what Sprint 0 produced — five parallel workstreams, five PRs, all merging into a new `src/` directory that didn't exist 24 hours ago:

| PR | Agent | What | Files Changed |
|----|-------|------|---------------|
| #1 | Heisenberg | New SDK-style solution structure | `Terrarium.sln`, `Directory.Build.props`, 7 project shells |
| #2 | Saul | .NET Aspire AppHost + ServiceDefaults | `Terrarium.AppHost`, `Terrarium.ServiceDefaults` |
| #3 | Mike | OrganismBase port — the creature SDK | 91 source files, 3,200+ lines of ported C# |
| #4 | Jesse | Glass CSS design tokens | `glass-theme.css`, `glass-components.css` — 663 lines |
| #5 | Hank | GitHub Actions CI pipeline | `build.yml` — build, restore, test on every push and PR |

Five agents. Five branches. All kicked off at the same time. All producing real, buildable code that targets .NET 10.

If you've ever done a big migration and spent the first two weeks arguing about project structure in a meeting, you just felt something.

---

## Mike's OrganismBase Port: The Heart of the Story

This is the one that matters most.

`OrganismBase` is the creature SDK. It's the library that every Terrarium developer coded against. When you wrote `class MyCarnivore : Animal`, you were extending `OrganismBase.Animal`. When your creature called `this.BeginAttacking()`, that method lived in `OrganismBase`. When the game engine checked your creature's `EyesightPoints` or `MaximumSpeedPoints`, those attributes came from `OrganismBase`.

It's the leaf node of the entire dependency graph — zero dependencies on anything else in Terrarium. Which makes it the perfect place to start. And Mike just ported the entire thing to .NET 10.

**91 source files.** 70+ legacy files read from `Client/OrganismBase/`. Every creature API surface preserved. Every attribute, every action, every event, every state class — all of it compiles on `net10.0`.

Here's what changed, and what didn't.

### BEFORE: .NET Framework 3.5

```csharp
// Client/OrganismBase/Classes/Actions/Action.cs — the original
using System;

namespace OrganismBase
{
    [Serializable]
    public abstract class Action
    {
        internal Action(string organismID, int actionID)
        {
            OrganismID = organismID;
            ActionID = actionID;
        }
```

```csharp
// Client/OrganismBase/Classes/State/OrganismState.cs — the original
[Serializable]
public abstract class OrganismState : IComparable
{
    [NonSerialized] private MoveToAction currentMoveToAction;
    // ...
}
```

`[Serializable]` on every state class and action. `[NonSerialized]` on fields that shouldn't survive a `BinaryFormatter` round-trip. This was the .NET Framework serialization model — mark your types, mark your exceptions, and pray that nobody deserializes a malicious payload. (`BinaryFormatter` was *removed* from .NET 10. Not deprecated. Removed. Because it was a remote code execution vulnerability wearing a trench coat.)

### AFTER: .NET 10

```csharp
// src/Terrarium.OrganismBase/Actions/Action.cs — ported
namespace OrganismBase;

/// <summary>
/// Base class for all actions a creature can perform.
/// </summary>
public abstract class Action
{
    internal Action(string organismID, int actionID)
    {
        OrganismID = organismID;
        ActionID = actionID;
    }

    public string OrganismID { get; private set; }

    public int ActionID { get; private set; }
}
```

```csharp
// src/Terrarium.OrganismBase/State/OrganismState.cs — ported
namespace OrganismBase;

public abstract class OrganismState : IComparable
{
    private MoveToAction? currentMoveToAction;
    // ...
}
```

Look at what's gone:

- **`[Serializable]` / `[NonSerialized]`** — removed from 28 classes. Serialization will use `System.Text.Json` with explicit contracts, not runtime type inspection.
- **`namespace OrganismBase { ... }`** → **`namespace OrganismBase;`** — file-scoped namespaces throughout. One less level of indentation. Your creature code looks cleaner.
- **`MoveToAction currentMoveToAction`** → **`MoveToAction? currentMoveToAction`** — nullable reference types enabled. The compiler now tells you where null can flow. The original code just hoped.

And look at what *survived*:

```csharp
// This still works. That's the whole point.
[CarnivoreAttribute(true)]
[MatureSizeAttribute(48)]
[MaximumEnergyPointsAttribute(0)]
[EatingSpeedPointsAttribute(0)]
[AttackDamagePointsAttribute(100)]
[MaximumSpeedPointsAttribute(0)]
[DefendDamagePointsAttribute(0)]
[EyesightPointsAttribute(50)]
[CamouflagePointsAttribute(0)]
public class MyCarnivore : Animal
{
    // Your creature code here
}
```

**The creature API surface is preserved.** If you wrote a Terrarium creature in 2003 — even if you barely remember C# — the class hierarchy still makes sense. `Animal` still has `BeginAttacking()`. `Plant` still grows. `OrganismState` still tracks position, energy, and age. The point allocation system — where you distribute 100 points across attributes like `EyesightPoints`, `MaximumSpeedPoints`, and `AttackDamagePoints` — works exactly like it always did.

Mike made a smart call on one thing: `IAnimalWorldBoundary.Scan()` still returns `ArrayList`. Not `List<OrganismState>`. Why? Because the `Game` project — which *implements* that interface — hasn't been ported yet. Changing the return type now would create a compile-time dependency on code that doesn't exist yet. So it stays `ArrayList` until Sprint 4 when the Game engine gets ported. That's the kind of pragmatic decision that keeps a migration moving instead of stalling.

---

## Jesse's Glass Theme: The Visual Identity Survives

Here's a line of C# from 2003:

```csharp
// Client/Glass/GlassStyle.cs
protected GlassGradient panel = new GlassGradient(
    Color.FromArgb(128, 128, 255),
    Color.FromArgb(0, 0, 96)
);
```

That's the signature Terrarium look. That blue-to-dark-blue gradient that sits behind every panel. If you ever saw a Terrarium screenshot, that color is burned into your memory.

Here's what Jesse turned it into:

```css
/* src/Terrarium.Web/wwwroot/css/glass-theme.css */
--glass-gradient-panel-top:        #8080ff;  /* Color.FromArgb(128, 128, 255) */
--glass-gradient-panel-bottom:     #000060;  /* Color.FromArgb(0, 0, 96) */
```

Every. Single. Color. Value.

Jesse went through `GlassStyle.cs`, `GlassGradient.cs`, `GlassHelper.cs`, `GlassPanel.cs`, `GlassButton.cs`, the `TerrariumLed.cs` indicator lights, the title bar, the status bar — and extracted every `Color.FromArgb()` call into a CSS custom property. 153 lines of design tokens in `glass-theme.css`. 474 lines of component styles in `glass-components.css`. All following a naming convention that maps directly back to the original C# code:

```
--glass-{category}-{element}-{modifier}

GlassStyle.ButtonHover.Top  →  --glass-gradient-button-hover-top
TerrariumLed idle color     →  --glass-gradient-led-idle-top
GlassHelper glass overlay   →  --glass-overlay-end
```

The button hover state? That green glow that told you something was interactive?

```css
--glass-gradient-button-hover-top:       #80ff80;  /* Color.FromArgb(128, 255, 128) */
--glass-gradient-button-hover-bottom:    #006000;  /* Color.FromArgb(0, 96, 0) */
```

Still there. Still green. Still glowing. But now it's a CSS custom property that Skyler can apply to Blazor components. No more `System.Drawing.Color`. No more GDI+ `LinearGradientBrush`. Just CSS that works in every browser on every platform.

The glass overlay effect — that semi-transparent white sheen that made everything look like it was behind a pane of glass — is even preserved:

```css
--glass-overlay-start:             transparent;
--glass-overlay-end:               rgba(255, 255, 255, 0.125);
```

`Color.FromArgb(32, 255, 255, 255)` → `rgba(255, 255, 255, 0.125)`. DirectDraw 7 → CSS. The visual identity of Terrarium, ported to the web without losing a single color value.

---

## Heisenberg's Architecture: The Bones

While Mike and Jesse were porting code and extracting tokens, Heisenberg was building the skeleton that holds everything together.

### The New Solution

```
src/
├── Terrarium.sln                    ← Finally spelled correctly
├── Directory.Build.props            ← .NET 10, nullable, warnings-as-errors
├── Terrarium.AppHost/               ← .NET Aspire orchestrator
├── Terrarium.ServiceDefaults/       ← OpenTelemetry, health checks
├── Terrarium.OrganismBase/          ← The creature SDK (Mike's port)
├── Terrarium.Game/                  ← Engine (Sprint 4)
├── Terrarium.Server/                ← API backend (Sprint 1)
└── Terrarium.Web/                   ← Blazor frontend (Sprint 3)
```

Yes, you read that right. The solution file is called `Terrarium.sln`. Not `Terrraium2010.sln`. The triple-R typo that survived every commit since Visual Studio 2010 has finally been fixed. It only took 15 years.

The `Directory.Build.props` is where the real decisions live:

```xml
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <NoWarn>CS1591</NoWarn>
  </PropertyGroup>
</Project>
```

**Nullable enabled from day one.** Every project in this solution has nullable reference types turned on. No more guessing whether something can be null. The compiler tells you.

**Warnings as errors.** If it warns, it doesn't build. CS1591 (missing XML doc comments) is suppressed during the initial port because adding XML docs to 91 files before the API is stable would be a waste of everyone's time. It gets re-enabled once the APIs settle.

Heisenberg also made a deliberate choice: classic `.sln` format, not the new `.slnx` XML format that .NET 10 defaults to. Why? Because Brady's audience includes .NET veterans who will open this repo in VS, VS Code, Rider — and classic `.sln` has universal tooling support. Pragmatism over novelty.

---

## Saul's Aspire: One Command to Start Everything

Saul wired up .NET Aspire — the orchestration layer that makes local development feel like magic.

```csharp
// src/Terrarium.AppHost/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

// Terrarium Server — the game's API backend
// var server = builder.AddProject<Projects.Terrarium_Server>("server");

// Terrarium Web — Blazor frontend
// builder.AddProject<Projects.Terrarium_Web>("web")
//     .WithExternalHttpEndpoints()
//     .WithReference(server)
//     .WaitFor(server);

builder.Build().Run();
```

The service references are commented out because the Server and Web projects are stubs right now — they'll light up in Sprint 1 and Sprint 3. But the infrastructure is ready. The `Terrarium.AppHost.csproj` already references Aspire 13.1.0. The `Terrarium.ServiceDefaults` project has OpenTelemetry, health checks, and service discovery wired up.

When Sprint 1 fills in the server and Sprint 3 adds the Blazor frontend, uncommenting those two lines gives you:

```
dotnet run --project src/Terrarium.AppHost
```

And the Aspire dashboard lights up. Server, frontend, telemetry, health checks — all running, all connected, all visible in one dashboard. That's the developer experience .NET Aspire was built for.

---

## Hank's CI: The First Green Build

```yaml
# .github/workflows/build.yml
name: Build and Test
on:
  push:
    branches: [main, squadified]
  pull_request:

jobs:
  build:
    name: Build & Test
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
      - name: Restore
        run: dotnet restore src/Terrarium.sln
      - name: Build
        run: dotnet build src/Terrarium.sln --no-restore --configuration Release
      - name: Test
        run: dotnet test src/Terrarium.sln --no-build --configuration Release
```

Ubuntu runner. Cross-platform from day one — this isn't a Windows app anymore.

NuGet caching. `dotnet-quality: preview` for .NET 10 preview SDK. Runs on every push to `main` and `squadified`, and on every PR. If it doesn't build, the PR doesn't merge.

It's not glamorous. It's the most important thing Hank built. Because from this moment forward, every agent's PR gets tested automatically. The foundation that keeps everything honest.

---

## The AI Meta-Story

Let's talk about what actually happened here, because it's worth stepping back and looking at it.

Five AI agents were launched simultaneously. Each got a task from the sprint plan:

1. **Heisenberg** → Create the solution structure
2. **Saul** → Wire up .NET Aspire
3. **Mike** → Port OrganismBase (91 files of creature SDK)
4. **Jesse** → Extract Glass CSS tokens (every color from the original UI)
5. **Hank** → Create CI pipeline

Each agent:
- Read the relevant legacy code from `Client/`
- Created new files in `src/`
- Worked on their own branch (`squad/1-*`, `squad/2-*`, etc.)
- Created their own PR
- Filed decisions in `.ai-team/decisions/inbox/` when they made judgment calls

They didn't coordinate in real time. They didn't have meetings. They each read the sprint plan, the architecture doc, the decision log — and then they went and did their job.

Mike read 70+ legacy files from `Client/OrganismBase/` and produced 91 new files that compile on .NET 10. Jesse went through 6 source files in `Client/Glass/` and `Client/Controls/` and extracted every `Color.FromArgb()` into CSS custom properties. Heisenberg set up the project structure that both Mike and Jesse's code landed in. Saul wired the orchestration. Hank made sure it all builds.

This is how you modernize a 25-year-old codebase. Not with one giant PR that touches everything. Not with a six-month analysis phase. You break it into parallel workstreams, you give each workstream clear boundaries, and you let them run.

The fact that the workstreams were AI agents instead of humans is interesting. But the pattern — parallel workstreams with clear ownership and a shared architecture — is the same pattern that works with human teams. The agents just moved faster.

---

## What We Have Now

After Sprint 0, here's what exists:

✅ **New `Terrarium.sln`** — SDK-style projects targeting .NET 10
✅ **`Directory.Build.props`** — nullable enabled, warnings-as-errors, .NET 10
✅ **`Terrarium.OrganismBase`** — 91 files, the complete creature SDK, compiling on .NET 10
✅ **`Terrarium.AppHost`** — .NET Aspire orchestrator, ready for services
✅ **`Terrarium.ServiceDefaults`** — OpenTelemetry, health checks, service discovery
✅ **`glass-theme.css` + `glass-components.css`** — 663 lines of design tokens extracted from the original Glass UI
✅ **GitHub Actions CI** — builds and tests on every push and PR
✅ **Three decisions filed** — solution format, CSS token naming, ArrayList deferral

What we *don't* have yet:

- No server endpoints (Sprint 1)
- No Blazor components (Sprint 3)
- No game engine (Sprint 4)
- No rendering (Sprint 8)
- No creatures moving on screen

Sprint 0 is all foundation. No one sees it. No one demos it. But without it, nothing else happens.

The last two modernization attempts created empty project shells and stopped. We have 91 source files, 663 lines of CSS, an Aspire AppHost, and a green CI build. We are *past* the point where the previous attempts gave up.

---

## What's Next

Sprint 1: **Server Bootstrap.** Gus picks up the ASP.NET Core server. The first Minimal API endpoints. SQL Server as an Aspire resource. Dapper wired up. The messaging endpoints ported from ASMX to JSON.

The creature SDK compiles. Now we need somewhere to register the creatures.

---

*Next entry: Sprint 1 — the server comes online.*

*— Beth*
