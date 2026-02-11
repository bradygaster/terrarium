// Copyright (c) Microsoft Corporation.  All rights reserved.

namespace Terrarium.Configuration;

/// <summary>
/// Represents a single named profiling measurement.
/// </summary>
public sealed class ProfilerNode
{
    private readonly TimeMonitor _timeMonitor = new();
    private bool _isCounting;

    public ProfilerNode(string id)
    {
        Id = id;
    }

    public string Id { get; }

    /// <summary>Time consumed by the last call, in microseconds.</summary>
    public long LastTime { get; private set; }

    /// <summary>Total time consumed across all calls, in microseconds.</summary>
    public long RunningTotal { get; private set; }

    /// <summary>Number of completed timing samples.</summary>
    public int Samples { get; private set; }

    public void Start()
    {
        _isCounting = true;
        _timeMonitor.Start();
    }

    public void End()
    {
        if (!_isCounting) return;
        _isCounting = false;
        LastTime = _timeMonitor.EndGetMicroseconds();
        RunningTotal += LastTime;
        Samples++;
    }
}
