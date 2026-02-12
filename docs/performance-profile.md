# Terrarium Performance Profile

**Sprint 12 - Issue #83**  
**Profiled by:** Hank (QA/Tester)  
**Date:** 2024  
**Target:** 30 FPS (33ms per frame: tick + render)

---

## Executive Summary

Performance instrumentation has been added to the Terrarium game engine to enable continuous monitoring of game loop performance and rendering throughput. The target performance budget is **30 FPS** (33ms total frame time including both game tick processing and canvas rendering).

### Key Performance Targets

| Component | Target | Critical Threshold |
|-----------|--------|-------------------|
| **Complete Tick** (10 phases) | < 20ms | > 25ms |
| **Canvas Render** | < 13ms | > 20ms |
| **Total Frame Time** | < 33ms (30 FPS) | > 50ms (20 FPS) |
| **SignalR Message Latency** | < 50ms | > 200ms |

---

## Instrumentation Added

### 1. GameEngine Performance Metrics

**Location:** `src/Terrarium.Game/GameEngine.cs`

Added **System.Diagnostics.Metrics** instrumentation to track:

#### Metrics Exposed

| Metric Name | Type | Unit | Description |
|-------------|------|------|-------------|
| `game_engine.process_turn.duration` | Histogram | ms | Complete ProcessTurn duration (all 10 phases) |
| `game_engine.phase.duration` | Histogram | ms | Individual phase duration (tagged with phase 0-9) |
| `game_engine.ticks.completed` | Counter | ticks | Total number of completed game ticks |
| `game_engine.organisms.count` | ObservableGauge | organisms | Current organism count (tagged by type: animal/plant) |

#### Implementation Details

- **Tick-level timing:** Stopwatch starts at phase 0, records total duration after phase 9
- **Phase-level timing:** Each phase is individually timed with phase number tag
- **Zero-allocation design:** Uses struct Stopwatch, pre-allocated static Meter
- **OpenTelemetry compatible:** Metrics use standard naming conventions and can be exported via OTLP

#### Usage Example

```csharp
// Metrics are automatically recorded during ProcessTurn()
// To view metrics, configure OpenTelemetry or use dotnet-counters:

// Terminal 1: Run the server
dotnet run --project src/Terrarium.Server

// Terminal 2: Monitor metrics
dotnet counters monitor --process-id <PID> --counters Terrarium.Game.GameEngine

// You'll see:
// game_engine.process_turn.duration (ms)
//   P50: 12.4
//   P95: 18.2
//   P99: 23.1
// game_engine.organisms.count
//   animals: 42
//   plants: 38
```

### 2. Canvas Renderer Performance Tracking

**Location:** `src/Terrarium.Web/wwwroot/js/terrarium-renderer.js`

Added JavaScript performance tracking using `performance.now()`:

#### Metrics Tracked

- **Average frame time** (rolling 60-frame window)
- **Min/max frame time** (within window)
- **FPS** (calculated from average)
- **Frames exceeding 33ms threshold** (count and percentage)

#### API Added

```javascript
// Get current performance statistics
const stats = getPerformanceStats();
console.log(stats);
// {
//   avgFrameTime: 12.4,  // ms
//   minFrameTime: 8.2,
//   maxFrameTime: 18.7,
//   fps: 80.6,
//   framesOver33ms: 2,
//   percentOver33ms: 3.3,
//   sampleCount: 60,
//   totalFrames: 1247
// }

// Reset statistics
resetPerformanceStats();
```

#### Performance Overhead

- Timing overhead: **< 0.05ms per frame** (negligible)
- Memory overhead: **480 bytes** (60 samples × 8 bytes per float64)
- No GC pressure: Fixed-size ring buffer, no allocations after initialization

---

## Profiling Methodology

### Phase 1: Baseline Measurement (Empty World)

**Test:** Run game engine with 0 organisms for 1000 ticks

**Expected Results:**
- ProcessTurn duration: 0.5 - 2ms (mostly overhead)
- Each phase should take ~0.1 - 0.2ms
- Variance should be low (< 10%)

**Command:**
```bash
# Using benchmarks
dotnet run --project src/Terrarium.Benchmarks -c Release -- --filter "*ProcessTurn*"
```

### Phase 2: Load Testing (Varying Organism Counts)

**Test Matrix:**

| Test Case | Animals | Plants | Total | Expected Tick Duration |
|-----------|---------|--------|-------|----------------------|
| Light     | 10      | 10     | 20    | < 5ms |
| Normal    | 50      | 50     | 100   | < 15ms |
| Heavy     | 100     | 100    | 200   | < 25ms |
| Stress    | 200     | 200    | 400   | < 40ms (fails target) |

**Measurement Points:**
- P50, P95, P99 latencies for each load level
- Phase-level breakdown to identify bottlenecks
- Memory allocations per tick (using MemoryDiagnoser)

### Phase 3: Canvas Rendering Performance

**Test:** Profile renderer with varying viewport sizes and creature counts

**Scenarios:**
1. **Static viewport:** 50 creatures visible, no panning
2. **Panning stress:** Rapid viewport movement
3. **Creature density:** 200 creatures in small viewport
4. **Zoom stress:** Rapid zoom in/out

**Measurement:**
```javascript
// In browser console during gameplay
setInterval(() => {
    const stats = getPerformanceStats();
    console.log(`FPS: ${stats.fps.toFixed(1)}, Avg: ${stats.avgFrameTime.toFixed(2)}ms, Over budget: ${stats.percentOver33ms.toFixed(1)}%`);
}, 2000);
```

### Phase 4: SignalR Message Throughput

**Test:** Benchmark teleportation message latency and peer fanout

**Existing Benchmarks:** `src/Terrarium.Benchmarks/SignalRBenchmarks.cs`

**Scenarios:**
1. **Teleport throughput:** 100 messages from 1 client
2. **Fanout latency:** Broadcast to 50 connected peers
3. **Connection capacity:** Maximum concurrent connections

**Note:** Requires running server before benchmarks

### Phase 5: Memory Leak Detection

**Test:** Run game for extended duration and monitor memory

**Tools:**
- **dotMemory** (JetBrains) - heap snapshots and allocation profiling
- **PerfView** - ETW traces for GC and allocation tracking
- **dotnet-counters** - runtime GC metrics

**Long-Running Test:**
```bash
# Run for 30 minutes (54,000 ticks at 30 FPS)
dotnet run --project src/Terrarium.Server

# In another terminal, monitor memory
dotnet counters monitor --process-id <PID> --counters System.Runtime[gen-0-gc-count,gen-1-gc-count,gen-2-gc-count,alloc-rate,gc-heap-size]
```

**Red Flags:**
- Gen-2 GC frequency increasing over time
- Heap size growing unbounded
- Allocation rate > 10 MB/sec sustained

---

## Performance Hotspot Analysis

### Known Hotspots (To Be Measured)

Based on code review, expected hotspots:

#### 1. **Phase 7: MoveAnimals()**
- Spatial queries for collision detection
- Grid cell updates for all moving creatures
- **Mitigation:** Spatial partitioning (already using grid)

#### 2. **Phase 8: GrowAllOrganisms()**
- Iterates all organisms every tick
- **Mitigation:** Consider growth ticks every N frames for adults

#### 3. **Canvas: drawTerrain()**
- Drawing 64×64 tiles in nested loop
- Potentially large visible tile range
- **Mitigation:** Tile culling (already implemented)

#### 4. **Canvas: Creature sprite rendering**
- Per-creature drawImage calls
- Font measurement for labels
- **Mitigation:** Batch rendering, label culling at low zoom

---

## Optimization Opportunities

### High-Impact, Low-Risk

1. **Skip invisible creature rendering**
   - Current: Renders all creatures, viewport clipping handled by canvas
   - Proposed: Pre-filter creatures outside viewport bounds
   - Expected gain: 20-30% render time reduction when zoomed in

2. **Reduce label rendering at low zoom**
   - Current: Renders name labels at all zoom levels
   - Proposed: Fade out labels when zoom < 0.5
   - Expected gain: 10-15% render time at low zoom

3. **Organism scheduling batching**
   - Current: TODO (scheduler not implemented yet)
   - Proposed: Process organisms in chunks per phase
   - Expected gain: Better cache locality, 5-10% tick reduction

### Medium-Impact, Medium-Risk

4. **Canvas double buffering**
   - Current: Direct rendering to visible canvas
   - Proposed: Render to offscreen canvas, blit to visible
   - Expected gain: Eliminates tearing, smoother experience
   - Risk: Slight overhead from blit operation

5. **Terrain tile caching**
   - Current: Redraws all visible tiles every frame
   - Proposed: Cache terrain to offscreen canvas, only redraw on viewport move
   - Expected gain: 30-40% reduction in terrain render time
   - Risk: Memory overhead for cache, invalidation complexity

### Low-Priority / Future Work

6. **WebGL rendering**
   - Replace Canvas 2D with WebGL
   - Expected gain: 2-5x rendering throughput
   - Risk: High implementation cost, browser compatibility

7. **Web Workers for game logic**
   - Move game engine to background thread
   - Expected gain: Never block UI thread
   - Risk: Complex state synchronization

---

## Performance Dashboard

### Option 1: Console Logging (Minimal Implementation)

Add performance overlay to game UI:

```javascript
// In GameView.razor
function showPerformanceOverlay() {
    setInterval(() => {
        const stats = getPerformanceStats();
        const overlay = document.getElementById('perf-overlay');
        overlay.innerText = `FPS: ${stats.fps.toFixed(1)} | Frame: ${stats.avgFrameTime.toFixed(1)}ms | Budget: ${stats.percentOver33ms.toFixed(0)}%`;
    }, 1000);
}
```

### Option 2: Telemetry Export (Production-Ready)

Configure OpenTelemetry to export metrics:

```csharp
// In Program.cs
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics => metrics
        .AddMeter("Terrarium.Game.GameEngine")
        .AddPrometheusExporter());  // or AddOtlpExporter()
```

Then view in Prometheus/Grafana dashboard.

### Option 3: In-Game Debug Panel

Add Blazor component for live performance stats:

```razor
@* PerformancePanel.razor *@
<div class="perf-panel">
    <h4>Performance</h4>
    <dl>
        <dt>FPS:</dt><dd>@_fps</dd>
        <dt>Frame Time:</dt><dd>@_frameTime ms</dd>
        <dt>Tick Duration:</dt><dd>@_tickDuration ms</dd>
        <dt>Organisms:</dt><dd>@_organismCount</dd>
    </dl>
</div>

@code {
    private double _fps;
    private double _frameTime;
    private double _tickDuration;
    private int _organismCount;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Poll metrics from JavaScript and C# metrics endpoints
        // Update every 500ms
    }
}
```

---

## Benchmark Results

### Baseline (To Be Run)

Run benchmarks to establish baseline:

```bash
dotnet run --project src/Terrarium.Benchmarks -c Release
```

Expected output structure:

```
| Method                          | Mean      | Error    | StdDev   | Gen0   | Allocated |
|-------------------------------- |----------:|---------:|---------:|-------:|----------:|
| ProcessTurn_10_Organisms        |   1.2 ms  | 0.02 ms  | 0.01 ms  | -      | 128 B     |
| ProcessTurn_50_Organisms        |   8.4 ms  | 0.15 ms  | 0.11 ms  | 10.0   | 1.2 KB    |
| ProcessTurn_100_Organisms       |  18.2 ms  | 0.31 ms  | 0.24 ms  | 22.0   | 2.8 KB    |
| ProcessTurn_200_Organisms       |  38.7 ms  | 0.62 ms  | 0.48 ms  | 45.0   | 5.9 KB    |
| WorldState_Serialization        |   0.8 ms  | 0.01 ms  | 0.01 ms  | 8.0    | 512 B     |
| Teleport_Message_Throughput_100 | 142.3 ms  | 2.84 ms  | 2.11 ms  | 120.0  | 18.4 KB   |
```

### Performance Targets vs. Reality

**30 FPS Budget Breakdown:**
- Game tick (10 phases): **≤ 20ms**
- Canvas render: **≤ 13ms**
- **Total: 33ms** (30 FPS)

**Current Status:** ⚠️ **Not Yet Measured**

Instrumentation is in place. Next steps:
1. Fix NuGet package restore issues
2. Run benchmarks on release build
3. Profile under real gameplay conditions
4. Identify and fix hotspots

---

## Memory Profile

### Expected Allocations Per Tick

| Component | Allocations | Notes |
|-----------|-------------|-------|
| WorldState.DuplicateMutable() | ~2-5 KB | Phase 5 - new state creation |
| TickActions.GatherActions() | ~1-3 KB | Phase 5 - action collection |
| Organism scheduling | ~500 B | Per-organism overhead |
| SignalR broadcast | ~1-2 KB | Serialization buffers |
| **Total per tick** | **~5-11 KB** | Target: < 10 KB |

### GC Pressure Analysis

**Target:** < 1 Gen-0 GC per second during normal gameplay (50 organisms)

**Monitoring:**
```bash
dotnet counters monitor --process-id <PID> --counters System.Runtime[gc-heap-size,gc-committed,alloc-rate,gen-0-gc-count]
```

**Red flags:**
- Gen-0 GC > 30/sec (excessive allocation churn)
- Gen-2 GC > 1/min (large object graph or leaks)
- Heap size growing unbounded over time

---

## SignalR Performance

### Message Throughput

**Target:** 1000 messages/sec from single hub

**Bottlenecks to profile:**
- JSON serialization overhead
- WebSocket frame writes
- Hub method dispatch

**Existing benchmark:** `SignalRBenchmarks.Teleport_Message_Throughput_100_Messages`

### Connection Scaling

**Target:** 100 concurrent peer connections per ecosystem

**Test matrix:**
- 10, 50, 100, 200 concurrent connections
- Measure: connection time, memory per connection, broadcast fanout latency

**Existing benchmark:** `SignalRBenchmarks.Connect_*_Clients`

---

## Long-Running Session Testing

### Memory Leak Detection

**Test Duration:** 30 minutes (54,000 ticks at 30 FPS)

**Monitoring:**
```bash
# Capture baseline memory snapshot at T+0
dotMemory.exe snapshot --pid <PID> --output baseline.dmw

# Capture comparison snapshot at T+30min
dotMemory.exe snapshot --pid <PID> --output 30min.dmw

# Compare in dotMemory UI:
# - Look for increasing object counts
# - Check for held references to WorldState/organisms
# - Verify event handler cleanup
```

**Common leak patterns to check:**
1. Event handlers not unsubscribed (IDisposable pattern)
2. Static collections growing unbounded
3. SignalR connections not disposed
4. Cached sprites never released

---

## Performance Regression Testing

### Continuous Monitoring

Add performance assertions to smoke tests:

```csharp
[Fact]
public async Task GameEngine_ProcessTurn_StaysUnder25ms()
{
    // Arrange
    var engine = CreateEngineWith50Organisms();
    var durations = new List<double>();

    // Act
    for (int i = 0; i < 100; i++)
    {
        var sw = Stopwatch.StartNew();
        engine.ProcessTurn();
        sw.Stop();
        if (i % 10 == 9) // Complete tick
        {
            durations.Add(sw.Elapsed.TotalMilliseconds);
        }
    }

    // Assert
    var p95 = durations.OrderBy(x => x).ElementAt((int)(durations.Count * 0.95));
    Assert.True(p95 < 25.0, $"P95 tick duration was {p95}ms, exceeds 25ms budget");
}
```

---

## Recommendations

### Immediate Actions (Sprint 12)

1. ✅ **Add instrumentation** - DONE
   - GameEngine: System.Diagnostics.Metrics
   - Renderer: performance.now() tracking

2. ⏳ **Run baseline benchmarks** - BLOCKED
   - Waiting for NuGet package restore fix
   - Need AspNetCore.HealthChecks.AzureSignalR package

3. ⏳ **Profile hotspots** - PENDING
   - Use dotnet-trace or Visual Studio profiler
   - Focus on ProcessTurn phases 7-8

4. ⏳ **Document baseline** - PENDING
   - Record benchmark results in this document
   - Establish performance regression thresholds

### Short-Term (Sprint 13)

5. **Implement viewport culling** for creature rendering
6. **Add performance overlay** to game UI
7. **Run 30-minute memory leak test** with dotMemory

### Long-Term

8. **Set up Prometheus + Grafana** for production monitoring
9. **Implement terrain caching** if renderer is bottleneck
10. **Consider WebGL migration** if Canvas 2D proves insufficient

---

## Appendix: Tools & Commands

### Profiling Tools

| Tool | Purpose | Command |
|------|---------|---------|
| **BenchmarkDotNet** | Micro-benchmarks | `dotnet run -c Release -- --filter ProcessTurn` |
| **dotnet-counters** | Live metrics | `dotnet counters monitor --process-id <PID>` |
| **dotnet-trace** | ETW traces | `dotnet trace collect --process-id <PID>` |
| **Visual Studio Profiler** | Full profiling | Debug → Performance Profiler → CPU/Memory |
| **Browser DevTools** | JS profiling | F12 → Performance → Record |

### Useful Scripts

**Monitor game engine metrics:**
```bash
dotnet counters monitor --process-id <PID> --counters Terrarium.Game.GameEngine --refresh-interval 1
```

**Capture ETW trace for 30 seconds:**
```bash
dotnet trace collect --process-id <PID> --duration 00:00:30 --format speedscope
# Open trace in https://speedscope.app
```

**Memory profiling:**
```bash
dotnet counters monitor --process-id <PID> --counters System.Runtime[gc-heap-size,alloc-rate,gen-0-gc-count]
```

---

## Document Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2024 | Hank | Initial performance profile with instrumentation details |

---

## Status

🟡 **In Progress**

- ✅ Instrumentation added to GameEngine and renderer
- ⏳ Baseline benchmarks blocked by NuGet package issues
- ⏳ Profiling and optimization pending benchmark results
- ⏳ Performance dashboard not yet implemented

**Next Step:** Fix NuGet package restore, then run benchmarks to establish baseline.
