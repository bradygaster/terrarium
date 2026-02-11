# Decision: Organism Isolation Architecture

**Author:** Heisenberg (Lead Architect)
**Date:** 2025-07-16
**Status:** Proposed
**Issue:** #43

## Context

The legacy Terrarium used `AppDomain`-based isolation (`OrganismWrapper`, `TerrariumOrganism`, `GameScheduler`) to execute untrusted creature code safely. .NET 10 removes AppDomains entirely. We need a modern replacement that provides:

1. **Assembly isolation** - creature assemblies must not interfere with each other or the host
2. **Safe execution** - timeout enforcement, exception containment, CPU fairness
3. **Memory management** - ability to unload creature code when species are removed
4. **Static analysis** - pre-load validation to reject obviously dangerous assemblies

## Decision

We implement a three-layer isolation architecture using **AssemblyLoadContext** as the primary isolation boundary:

### Layer 1: Static Validation (`CreatureValidator`)
Before any assembly is loaded, we perform metadata-level inspection using `System.Reflection.Metadata` to detect:
- P/Invoke declarations (native interop)
- Forbidden type references (file I/O, networking, process spawning, reflection emit)
- Missing required base class (must inherit `Animal` or `Plant`)
- Missing required attributes (`AuthorInformationAttribute`)

### Layer 2: Load Isolation (`OrganismSandbox`)
Each creature assembly type gets its own **collectible `AssemblyLoadContext`**:
- Assemblies are loaded from `byte[]` (no file locks)
- Contexts are collectible (`isCollectible: true`), enabling full unload
- Shared framework types (OrganismBase, BCL) resolve from the default context
- When a species is removed, its context is unloaded to reclaim memory

### Layer 3: Execution Safety (`OrganismHost` + `OrganismScheduler`)
- `OrganismHost.ExecuteTurn()` wraps each creature's `InternalMain()` with:
  - `CancellationTokenSource` timeout
  - `Task.Run` + `Task.Wait` for wall-clock enforcement
  - Full exception containment
  - CPU time measurement via `Stopwatch`
- `OrganismScheduler` provides fair round-robin scheduling:
  - Organisms divided into 5 batches (one per engine phase 0-4)
  - `OrganismQuanta` tracks per-creature CPU usage
  - Creatures exceeding quantum get reduced time in subsequent ticks

## Alternatives Considered

### Worker Process Isolation
**Pros:** True OS-level isolation, can enforce memory limits via process boundaries, crash containment
**Cons:**
- Massive serialization overhead (OrganismState, PendingActions, WorldState must cross process boundary every tick)
- 10-phase ProcessTurn design assumes in-process organism access
- Latency: 5 ticks/sec x hundreds of organisms x IPC round-trips = unacceptable
- Complexity: process lifecycle management, crash recovery, shared memory regions

**Verdict:** Too much overhead for the Terrarium's tight game loop. The legacy system was in-process for good reason.

### WebAssembly (WASM) Sandbox
**Pros:** Strong isolation, deterministic execution
**Cons:**
- Creatures are .NET assemblies; would require compilation to WASM
- No mature .NET-to-WASM-sandbox runtime for server-side use
- Would break the existing creature development workflow

**Verdict:** Interesting future direction but not practical today.

### Roslyn Scripting API
**Pros:** Fine-grained API restriction
**Cons:**
- Creatures are compiled assemblies, not scripts
- Would require rewriting the creature development model
- Performance overhead of interpretation

**Verdict:** Wrong abstraction level.

## Security Tradeoffs

| Risk | Mitigation | Residual Risk |
|------|-----------|---------------|
| Creature uses reflection to bypass restrictions | `CreatureValidator` blocks `System.Reflection.Emit`; runtime restrictions via ALC | Medium |
| CPU starvation (infinite loop) | `OrganismHost` timeout + `OrganismQuanta` penalties | Low |
| Memory exhaustion | Collectible ALC enables unload; future: per-creature memory tracking | Medium |
| Thread spawning | `CreatureValidator` blocks `System.Threading` namespace | Medium |
| File system access | `CreatureValidator` blocks `System.IO` namespace | Low |

## Future Considerations

1. **Process isolation (Phase 2):** For tournament/competitive scenarios, wrap the ALC-based sandbox inside a worker process for true containment. The `IOrganismScheduler` interface makes this swappable.
2. **Memory budgets:** Add per-ALC memory tracking using `AssemblyLoadContext` events and GC notifications.
3. **IL rewriting:** Use Cecil/Mono.Cecil to rewrite creature IL at load time, injecting cancellation checks at loop back-edges for cooperative cancellation.
4. **Allowlist approach:** Instead of blocking known-bad APIs, switch to an allowlist of permitted types for stronger guarantees.
5. **Runtime CAS replacement:** Investigate `System.Security.Permissions` polyfill or custom security transparent enforcement.

## Implementation Files

- `src/Terrarium.Game/Hosting/CreatureValidator.cs` - Static assembly validation + ValidationResult
- `src/Terrarium.Game/Hosting/OrganismSandbox.cs` - AssemblyLoadContext isolation
- `src/Terrarium.Game/Hosting/OrganismHost.cs` - Safe turn execution with timeout
- `src/Terrarium.Game/Hosting/OrganismQuanta.cs` - CPU time tracking and penalties
- `src/Terrarium.Game/Hosting/IOrganismScheduler.cs` - Scheduler interface
- `src/Terrarium.Game/Hosting/OrganismScheduler.cs` - Fair round-robin implementation
