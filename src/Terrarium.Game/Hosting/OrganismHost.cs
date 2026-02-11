// Copyright (c) Microsoft Corporation.  All rights reserved.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OrganismBase;

namespace Terrarium.Game.Hosting;

/// <summary>
/// The result of executing a creature's turn.
/// </summary>
public sealed class OrganismTurnResult
{
    /// <summary>The actions the creature wants to perform.</summary>
    public PendingActions Actions { get; init; } = new();

    /// <summary>CPU time consumed during the turn.</summary>
    public TimeSpan CpuTimeUsed { get; init; }

    /// <summary>Whether the creature's turn completed successfully.</summary>
    public bool Completed { get; init; }

    /// <summary>If the turn failed, the reason why.</summary>
    public string? ErrorMessage { get; init; }

    /// <summary>Whether the creature exceeded its time quantum.</summary>
    public bool TimedOut { get; init; }
}

/// <summary>
/// Wraps creature execution with safety controls.
/// Runs the creature's think method with timeout enforcement,
/// exception handling, and CPU time measurement.
/// </summary>
public sealed class OrganismHost
{
    private readonly ILogger<OrganismHost> _logger;

    public OrganismHost(ILogger<OrganismHost> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Executes a creature's turn (calls InternalMain) with timeout and exception safety.
    /// </summary>
    /// <param name="organism">The organism to execute.</param>
    /// <param name="timeout">Maximum wall-clock time allowed for the turn.</param>
    /// <param name="clearOnly">If true, the creature clears state without acting.</param>
    /// <returns>The result of the turn execution.</returns>
    public OrganismTurnResult ExecuteTurn(Organism organism, TimeSpan timeout, bool clearOnly = false)
    {
        if (organism == null)
            throw new ArgumentNullException(nameof(organism));

        var sw = Stopwatch.StartNew();
        using var cts = new CancellationTokenSource(timeout);

        try
        {
            var task = Task.Run(() => organism.InternalMain(clearOnly), cts.Token);

            if (task.Wait(timeout))
            {
                sw.Stop();
                var actions = organism.GetThenErasePendingActions();
                return new OrganismTurnResult
                {
                    Actions = actions,
                    CpuTimeUsed = sw.Elapsed,
                    Completed = true
                };
            }
            else
            {
                sw.Stop();
                cts.Cancel();
                _logger.LogWarning("Organism {ID} exceeded time quantum ({Timeout}ms)",
                    organism.ID, timeout.TotalMilliseconds);

                var actions = organism.GetThenErasePendingActions();
                return new OrganismTurnResult
                {
                    Actions = actions,
                    CpuTimeUsed = sw.Elapsed,
                    Completed = false,
                    TimedOut = true,
                    ErrorMessage = $"Turn exceeded time quantum of {timeout.TotalMilliseconds}ms"
                };
            }
        }
        catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
        {
            sw.Stop();
            _logger.LogWarning("Organism {ID} turn was cancelled", organism.ID);
            var actions = organism.GetThenErasePendingActions();
            return new OrganismTurnResult
            {
                Actions = actions,
                CpuTimeUsed = sw.Elapsed,
                Completed = false,
                TimedOut = true,
                ErrorMessage = "Turn was cancelled due to timeout"
            };
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Organism {ID} threw an exception during its turn", organism.ID);
            var actions = organism.GetThenErasePendingActions();
            return new OrganismTurnResult
            {
                Actions = actions,
                CpuTimeUsed = sw.Elapsed,
                Completed = false,
                ErrorMessage = $"Creature exception: {ex.GetBaseException().Message}"
            };
        }
    }
}
