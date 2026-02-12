# Terrarium Performance Baselines

Sprint 11 Issue #77 — Established performance benchmarks for load and stress testing.

## Overview

This document records baseline performance metrics for the Terrarium ecosystem. These benchmarks help identify regressions and guide optimization work.

## Test Environment

**Hardware:** TBD (record after first run)
**Software:**
- .NET 10.0
- BenchmarkDotNet 0.14.x
- OS: Windows/Linux/macOS

## Game Engine Performance

### ProcessTurn Duration

The core game loop's `ProcessTurn()` method processes all organisms each tick. Target: 60 FPS (16.67ms per frame).

| Organism Count | Mean Duration | StdDev | Allocation | Status |
|----------------|---------------|--------|------------|--------|
| 10             | TBD           | TBD    | TBD        | ⏳ Pending |
| 50 (default)   | TBD           | TBD    | TBD        | ⏳ Pending |
| 100            | TBD           | TBD    | TBD        | ⏳ Pending |
| 200            | TBD           | TBD    | TBD        | ⏳ Pending |

**Target:** 
- 50 organisms: < 16ms (60 FPS)
- 100 organisms: < 33ms (30 FPS acceptable for stress)

### WorldState Serialization

Serialization of WorldState to JSON for SignalR broadcasts.

| Scenario | Mean Duration | StdDev | Allocation | Status |
|----------|---------------|--------|------------|--------|
| Empty world | TBD        | TBD    | TBD        | ⏳ Pending |
| 50 organisms | TBD       | TBD    | TBD        | ⏳ Pending |

**Target:** < 5ms for 50 organisms

## SignalR Hub Performance

### Connection Capacity

How many concurrent SignalR connections can the hub handle?

| Client Count | Connection Time | Memory Usage | Status |
|--------------|----------------|--------------|--------|
| 10           | TBD            | TBD          | ⏳ Pending |
| 50           | TBD            | TBD          | ⏳ Pending |
| 100          | TBD            | TBD          | ⏳ Pending |

**Target:** 100+ concurrent clients with < 2s connection time

### Message Throughput

Teleport messages per second the hub can process.

| Scenario | Messages/sec | Latency (p50) | Latency (p99) | Status |
|----------|--------------|---------------|---------------|--------|
| 5 clients, 100 msgs | TBD | TBD      | TBD           | ⏳ Pending |
| 10 clients, 100 msgs | TBD | TBD     | TBD           | ⏳ Pending |

**Target:** 1000+ messages/sec with p99 latency < 100ms

### Fanout Performance

Broadcast latency when announcing to N peers in an ecosystem.

| Peer Count | Fanout Latency | Status |
|------------|----------------|--------|
| 10         | TBD            | ⏳ Pending |
| 50         | TBD            | ⏳ Pending |

**Target:** < 50ms for 50 peers

## Multi-Client Ecosystem Tests

End-to-end tests validating ecosystem behavior across N simulated browser clients.

| Test Scenario | Status | Notes |
|---------------|--------|-------|
| 3 clients see PeerAnnounce | ✅ Passing | All peers notified on join |
| Teleport broadcast to 3 clients | ✅ Passing | Creature visible across clients |
| Targeted teleport exclusivity | ✅ Passing | Only target receives creature |
| 5 clients RequestPeerList consistency | ✅ Passing | All get same ecosystem data |
| Leave broadcasts to remaining clients | ✅ Passing | All notified on peer leave |
| Assembly payload delivery | ✅ Passing | Binary data transmitted correctly |
| 10 clients load test | ✅ Passing | System handles 10 concurrent clients |

## Running the Benchmarks

### Game Engine Benchmarks

```bash
dotnet run -c Release --project src/Terrarium.Benchmarks
```

### SignalR Benchmarks

```bash
# Terminal 1: Start server
dotnet run --project src/Terrarium.Server

# Terminal 2: Run benchmarks
dotnet run -c Release --project src/Terrarium.Benchmarks
```

### Multi-Client Tests

```bash
# Local test run
dotnet test src/Terrarium.MultiClient.Tests

# CI environment with Docker Compose
docker-compose -f docker-compose.test.yml up --build --abort-on-container-exit
```

## Update Schedule

This document should be updated:
- After each sprint when new performance work lands
- When baselines regress by > 10%
- After infrastructure changes (new hardware, .NET version bumps)

## Notes

- **TBD values:** Populated after first benchmark run
- **Status indicators:** ⏳ Pending | ✅ Passing | ⚠️ Degraded | ❌ Failing
- All benchmarks run in Release mode with optimizations enabled
- SignalR benchmarks require a running server (not automated yet)

---

Last updated: Sprint 11
Next review: Sprint 12
