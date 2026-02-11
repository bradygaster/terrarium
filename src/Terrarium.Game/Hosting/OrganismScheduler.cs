// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using OrganismBase;

namespace Terrarium.Game.Hosting;

/// <summary>
/// Fair round-robin scheduler for organism turn execution.
/// Distributes CPU time across all organisms, enforcing time
/// quanta and penalizing creatures that exceed their allocation.
/// </summary>
public sealed class OrganismScheduler : IOrganismScheduler, IDisposable
{
    /// <summary>
    /// Number of ticks (Tick() calls) per game tick.
    /// The game engine calls Tick() during phases 0-4.
    /// </summary>
    public const int TicksPerGameTick = 5;

    private readonly ILogger<OrganismScheduler> _logger;
    private readonly OrganismHost _host;
    private readonly ConcurrentDictionary<string, OrganismEntry> _organisms = new();
    private readonly List<string> _roundRobinOrder = new();
    private readonly object _scheduleLock = new();

    private int _tickIndex;
    private bool _disposed;

    public OrganismScheduler(ILogger<OrganismScheduler> logger, OrganismHost host)
    {
        _logger = logger;
        _host = host;
    }

    /// <inheritdoc />
    public IReadOnlyCollection<Organism> Organisms =>
        _organisms.Values.Select(e => e.Organism).ToArray();

    /// <inheritdoc />
    public WorldState? CurrentState { get; set; }

    /// <inheritdoc />
    public Organism? Create(Type creatureType, string id)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (creatureType == null) throw new ArgumentNullException(nameof(creatureType));
        if (string.IsNullOrEmpty(id)) throw new ArgumentException("ID cannot be null or empty.", nameof(id));

        try
        {
            var organism = (Organism?)Activator.CreateInstance(creatureType);
            if (organism == null)
            {
                _logger.LogError("Failed to create organism of type {Type}", creatureType.FullName);
                return null;
            }

            var entry = new OrganismEntry(organism, new OrganismQuanta());
            if (!_organisms.TryAdd(id, entry))
            {
                _logger.LogWarning("Organism with ID {ID} already exists", id);
                return null;
            }

            lock (_scheduleLock)
            {
                _roundRobinOrder.Add(id);
            }

            _logger.LogDebug("Created organism {ID} of type {Type}", id, creatureType.Name);
            return organism;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create organism of type {Type}", creatureType.FullName);
            return null;
        }
    }

    /// <inheritdoc />
    public void Remove(string id)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_organisms.TryRemove(id, out _))
        {
            lock (_scheduleLock)
            {
                _roundRobinOrder.Remove(id);
            }
            _logger.LogDebug("Removed organism {ID}", id);
        }
    }

    /// <inheritdoc />
    public void Tick()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        string[] currentBatch;
        lock (_scheduleLock)
        {
            if (_roundRobinOrder.Count == 0) return;

            // Divide organisms into TicksPerGameTick batches.
            // Each Tick() call processes one batch.
            var total = _roundRobinOrder.Count;
            var batchSize = Math.Max(1, (total + TicksPerGameTick - 1) / TicksPerGameTick);
            var start = _tickIndex * batchSize;

            if (start >= total)
            {
                _tickIndex = (_tickIndex + 1) % TicksPerGameTick;
                return;
            }

            var count = Math.Min(batchSize, total - start);
            currentBatch = _roundRobinOrder.GetRange(start, count).ToArray();
            _tickIndex = (_tickIndex + 1) % TicksPerGameTick;
        }

        foreach (var id in currentBatch)
        {
            if (!_organisms.TryGetValue(id, out var entry)) continue;

            var quanta = entry.Quanta;
            var effectiveQuantum = quanta.GetEffectiveQuantumMs();
            var timeout = TimeSpan.FromMilliseconds(effectiveQuantum);

            quanta.StartTiming();
            var result = _host.ExecuteTurn(entry.Organism, timeout);
            var (_, exceeded) = quanta.StopTiming(effectiveQuantum);

            if (exceeded)
            {
                _logger.LogDebug("Organism {ID} exceeded quantum (penalty={Penalty})",
                    id, quanta.PenaltyMultiplier);
            }

            if (!result.Completed && result.ErrorMessage != null)
            {
                _logger.LogWarning("Organism {ID}: {Error}", id, result.ErrorMessage);
            }
        }
    }

    /// <inheritdoc />
    public Organism? GetOrganism(string id)
    {
        _organisms.TryGetValue(id, out var entry);
        return entry?.Organism;
    }

    /// <inheritdoc />
    public OrganismQuanta? GetQuanta(string id)
    {
        _organisms.TryGetValue(id, out var entry);
        return entry?.Quanta;
    }

    /// <inheritdoc />
    public void Destroy()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        lock (_scheduleLock)
        {
            _roundRobinOrder.Clear();
        }
        _organisms.Clear();
        _logger.LogInformation("OrganismScheduler destroyed");
    }

    private sealed class OrganismEntry
    {
        public Organism Organism { get; }
        public OrganismQuanta Quanta { get; }

        public OrganismEntry(Organism organism, OrganismQuanta quanta)
        {
            Organism = organism;
            Quanta = quanta;
        }
    }
}
