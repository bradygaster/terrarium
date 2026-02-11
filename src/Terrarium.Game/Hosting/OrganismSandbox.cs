// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Logging;

namespace Terrarium.Game.Hosting;

/// <summary>
/// Isolates creature assemblies using AssemblyLoadContext.
/// Each creature type gets its own collectible load context,
/// enabling assembly unloading and memory reclamation.
/// </summary>
public sealed class OrganismSandbox : IDisposable
{
    private readonly ILogger<OrganismSandbox> _logger;
    private readonly ConcurrentDictionary<string, CreatureLoadContext> _contexts = new();
    private bool _disposed;

    public OrganismSandbox(ILogger<OrganismSandbox> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Loads a creature assembly from raw bytes into an isolated AssemblyLoadContext.
    /// Each creature type (keyed by assembly full name) gets its own context.
    /// </summary>
    /// <param name="assemblyBytes">The raw assembly bytes to load.</param>
    /// <returns>The loaded assembly within its isolated context.</returns>
    public Assembly LoadCreatureAssembly(byte[] assemblyBytes)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (assemblyBytes == null || assemblyBytes.Length == 0)
            throw new ArgumentException("Assembly bytes cannot be null or empty.", nameof(assemblyBytes));

        // Load into a new isolated context
        var context = new CreatureLoadContext($"Creature_{Guid.NewGuid():N}");
        Assembly assembly;
        using (var loadStream = new MemoryStream(assemblyBytes))
        {
            assembly = context.LoadFromStream(loadStream);
        }

        var key = assembly.FullName ?? assembly.GetName().Name ?? context.Name!;

        // Unload existing context for this assembly if present
        if (_contexts.TryRemove(key, out var existingContext))
        {
            _logger.LogInformation("Unloading previous context for {Assembly}", key);
            existingContext.Unload();
        }

        _contexts[key] = context;
        _logger.LogInformation("Loaded creature assembly '{Name}' into isolated context", key);

        return assembly;
    }

    /// <summary>
    /// Unloads the context for a specific assembly, freeing all associated memory.
    /// </summary>
    /// <param name="assemblyFullName">The full name of the assembly to unload.</param>
    /// <returns>True if the context was found and unloaded; false otherwise.</returns>
    public bool UnloadCreatureAssembly(string assemblyFullName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_contexts.TryRemove(assemblyFullName, out var context))
        {
            context.Unload();
            _logger.LogInformation("Unloaded creature context: {Assembly}", assemblyFullName);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the names of all currently loaded creature assemblies.
    /// </summary>
    public IReadOnlyCollection<string> LoadedAssemblies => _contexts.Keys.ToArray();

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        foreach (var kvp in _contexts)
        {
            try { kvp.Value.Unload(); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error unloading context {Assembly}", kvp.Key);
            }
        }
        _contexts.Clear();
    }

    /// <summary>
    /// A collectible AssemblyLoadContext that isolates creature code
    /// and can be unloaded to reclaim memory.
    /// </summary>
    private sealed class CreatureLoadContext : AssemblyLoadContext
    {
        public CreatureLoadContext(string name) : base(name, isCollectible: true) { }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // Return null to fall back to the default context for shared framework assemblies.
            // Creature assemblies should only reference OrganismBase and standard BCL types,
            // which are already loaded in the default context.
            return null;
        }
    }
}
