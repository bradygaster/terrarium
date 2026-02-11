// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Diagnostics;

namespace Terrarium.Game.Hosting;

/// <summary>
/// CPU time measurement for fair scheduling of organisms.
/// Each creature gets a time quantum per tick. This class tracks
/// cumulative CPU usage and applies penalties for overuse.
/// Ported from the legacy OrganismQuanta with modern APIs.
/// </summary>
public sealed class OrganismQuanta
{
    /// <summary>Default quantum in milliseconds per tick.</summary>
    public const int DefaultQuantumMs = 5;

    /// <summary>Maximum penalty multiplier for creatures that exceed their quantum.</summary>
    public const int MaxPenaltyMultiplier = 3;

    private readonly object _lock = new();
    private readonly Stopwatch _stopwatch = new();

    private long _totalTicksConsumed;
    private int _totalActivations;
    private long _lastTicksConsumed;
    private int _penaltyMultiplier;
    private long _overageTicks;

    /// <summary>Total CPU ticks consumed across all activations.</summary>
    public long TotalTicksConsumed { get { lock (_lock) return _totalTicksConsumed; } }

    /// <summary>CPU ticks consumed during the last activation.</summary>
    public long LastTicksConsumed { get { lock (_lock) return _lastTicksConsumed; } }

    /// <summary>Total number of activations (turns executed).</summary>
    public int TotalActivations { get { lock (_lock) return _totalActivations; } }

    /// <summary>Cumulative overage ticks beyond the quantum.</summary>
    public long OverageTicks { get { lock (_lock) return _overageTicks; } }

    /// <summary>Current penalty multiplier (0 = no penalty).</summary>
    public int PenaltyMultiplier { get { lock (_lock) return _penaltyMultiplier; } }

    /// <summary>Average CPU time per activation.</summary>
    public TimeSpan AverageTimePerActivation
    {
        get
        {
            lock (_lock)
            {
                if (_totalActivations == 0) return TimeSpan.Zero;
                return TimeSpan.FromTicks(_totalTicksConsumed / _totalActivations);
            }
        }
    }

    /// <summary>
    /// Starts timing a creature's turn.
    /// </summary>
    public void StartTiming()
    {
        _stopwatch.Restart();
    }

    /// <summary>
    /// Stops timing and records the CPU usage for this turn.
    /// Returns the elapsed time and whether the quantum was exceeded.
    /// </summary>
    /// <param name="quantumMs">The allowed quantum in milliseconds.</param>
    /// <returns>A tuple of (elapsed time, exceeded quantum).</returns>
    public (TimeSpan elapsed, bool exceeded) StopTiming(int quantumMs = DefaultQuantumMs)
    {
        _stopwatch.Stop();
        var elapsed = _stopwatch.Elapsed;
        var elapsedTicks = _stopwatch.ElapsedTicks;

        lock (_lock)
        {
            _lastTicksConsumed = elapsedTicks;
            _totalTicksConsumed += elapsedTicks;
            _totalActivations++;

            var quantumTicks = (long)(quantumMs * Stopwatch.Frequency / 1000.0);
            if (elapsedTicks > quantumTicks)
            {
                _overageTicks += elapsedTicks - quantumTicks;
                _penaltyMultiplier = Math.Min(_penaltyMultiplier + 1, MaxPenaltyMultiplier);
                return (elapsed, true);
            }

            // Decay penalty when within quantum
            if (_penaltyMultiplier > 0)
                _penaltyMultiplier--;
        }

        return (elapsed, false);
    }

    /// <summary>
    /// Gets the effective quantum for this creature, accounting for penalties.
    /// Penalized creatures get a reduced quantum.
    /// </summary>
    /// <param name="baseQuantumMs">The base quantum in milliseconds.</param>
    /// <returns>The effective quantum after penalty adjustment.</returns>
    public int GetEffectiveQuantumMs(int baseQuantumMs = DefaultQuantumMs)
    {
        lock (_lock)
        {
            if (_penaltyMultiplier == 0) return baseQuantumMs;
            return Math.Max(1, baseQuantumMs / (_penaltyMultiplier + 1));
        }
    }

    /// <summary>
    /// Resets all tracking counters.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _totalTicksConsumed = 0;
            _totalActivations = 0;
            _lastTicksConsumed = 0;
            _penaltyMultiplier = 0;
            _overageTicks = 0;
        }
    }
}
