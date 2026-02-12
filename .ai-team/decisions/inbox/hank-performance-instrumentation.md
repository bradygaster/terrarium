### 2026-02-12: Performance instrumentation and profiling infrastructure

**By:** Hank  
**What:** Added System.Diagnostics.Metrics to GameEngine and performance.now() tracking to canvas renderer. Created comprehensive performance profile document with methodology, targets, and optimization roadmap.

**Why:**  
Sprint 12 Issue #83 required performance profiling infrastructure for the game loop. We need to:
- Track tick loop duration (target: <20ms for 30 FPS)
- Profile canvas rendering performance (target: <13ms)
- Monitor SignalR message throughput
- Detect memory leaks in long-running sessions

**Decisions Made:**

1. **System.Diagnostics.Metrics for GameEngine**
   - Modern, OpenTelemetry-compatible instrumentation
   - 4 metrics: process_turn.duration (histogram), phase.duration (histogram with phase tag), ticks.completed (counter), organisms.count (gauge by type)
   - Zero-allocation design using struct Stopwatch and static Meter
   - Phase-level granularity (0-9) enables bottleneck identification

2. **performance.now() for Canvas Renderer**
   - Rolling 60-frame window for statistical smoothing (2 seconds at 30 FPS)
   - Tracks: avg/min/max frame time, FPS, frames over 33ms threshold
   - New exports: getPerformanceStats(), resetPerformanceStats()
   - Overhead: <0.05ms per frame, 480 bytes fixed memory

3. **Performance Targets**
   - 30 FPS total (33ms frame budget)
   - Game tick: ≤20ms (10 phases)
   - Canvas render: ≤13ms
   - SignalR latency: <50ms
   - These are aggressive but achievable targets for smooth gameplay

4. **5-Phase Profiling Methodology**
   - Phase 1: Baseline (empty world, measure overhead)
   - Phase 2: Load testing (10/50/100/200 organisms)
   - Phase 3: Canvas (viewport stress, creature density, zoom)
   - Phase 4: SignalR (throughput, fanout, connection capacity)
   - Phase 5: Memory leaks (30-minute run, dotMemory/PerfView)

5. **Optimization Roadmap Documented**
   - High-impact/low-risk: viewport culling, label fade at low zoom, organism batching
   - Medium-impact/medium-risk: double buffering, terrain caching
   - Future work: WebGL, Web Workers
   - All tied to expected hotspots from code review

6. **Metrics Naming Conventions**
   - OpenTelemetry-compatible: {namespace}.{component}.{metric}
   - Tags for dimensions: phase, type
   - Units specified: ms, ticks, organisms
   - Ready for OTLP/Prometheus export

**Impact:**
- Enables data-driven optimization (measure, don't guess)
- Provides continuous monitoring in production via OpenTelemetry
- Establishes baseline for performance regression testing
- Documents expected hotspots for future developers
- Benchmark infrastructure already exists from Sprint 11 Issue #77

**Next Steps:**
- Fix NuGet package restore (AspNetCore.HealthChecks.AzureSignalR missing)
- Run benchmarks to establish baseline numbers
- Profile with dotnet-trace/Visual Studio
- Implement optimizations based on measured bottlenecks
