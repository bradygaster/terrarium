// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Collections.Concurrent;

namespace Terrarium.Configuration;

/// <summary>
/// Collects named profiling nodes for performance measurement.
/// </summary>
public sealed class Profiler
{
    private ConcurrentDictionary<string, ProfilerNode> _nodes = new();

    public ProfilerNode? this[string key]
        => _nodes.TryGetValue(key, out var node) ? node : null;

    public ProfilerNode[] Nodes => [.. _nodes.Values];

    public void ClearProfiler()
    {
        _nodes = new ConcurrentDictionary<string, ProfilerNode>();
    }

    public void Start(string functionName)
    {
        var node = _nodes.GetOrAdd(functionName, static name => new ProfilerNode(name));
        node.Start();
    }

    public void End(string functionName)
    {
        if (_nodes.TryGetValue(functionName, out var node))
        {
            node.End();
        }
    }
}
