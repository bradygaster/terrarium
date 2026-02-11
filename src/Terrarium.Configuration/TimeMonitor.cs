// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Diagnostics;

namespace Terrarium.Configuration;

/// <summary>
/// High-resolution timer using <see cref="Stopwatch"/> instead of the legacy
/// <c>QueryPerformanceCounter</c> P/Invoke.
/// </summary>
public sealed class TimeMonitor
{
    private readonly Stopwatch _stopwatch = new();

    /// <summary>
    /// Whether a timing session is currently in progress.
    /// </summary>
    public bool IsStarted => _stopwatch.IsRunning;

    /// <summary>
    /// Starts (or restarts) the timing session.
    /// </summary>
    public void Start()
    {
        _stopwatch.Restart();
    }

    /// <summary>
    /// Ends the timing session and returns the elapsed time in microseconds.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if Start was not called first.</exception>
    public long EndGetMicroseconds()
    {
        if (!_stopwatch.IsRunning)
        {
            throw new InvalidOperationException("Start must be called before calling End.");
        }

        _stopwatch.Stop();
        return _stopwatch.Elapsed.Ticks / (TimeSpan.TicksPerMillisecond / 1000);
    }

    /// <summary>
    /// Returns the total elapsed seconds since the last <see cref="Start"/> call without stopping.
    /// </summary>
    public double GetCounterSeconds()
    {
        return _stopwatch.Elapsed.TotalSeconds;
    }
}
