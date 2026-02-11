// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Diagnostics;
using Microsoft.Extensions.Logging;
using OrganismBase;
using Terrarium.Configuration;

namespace Terrarium.Game.Hosting;

/// <summary>
/// Manages creature execution within the game engine. Allocates time quanta
/// per organism per tick, runs their think methods, and tracks timing/overage
/// to penalize or blacklist creatures that exceed their budget.
/// </summary>
public sealed class GameScheduler : IDisposable
{
    // If an animal actually gets this much elapsed time in a single timeslice, we permanently blacklist them.
    private const int DeadlockThresholdMs = 5000; // 5 seconds

    // How often deadlock detection checks whether an animal has returned
    private const int DeadlockCheckIntervalMs = 7500;

    // How many retries before we restart without blacklisting
    private const int DeadlockRetries = 3;

    private const int ReportInterval = 150;

    private readonly ILogger<GameScheduler> _logger;
    private readonly TimeMonitor _monitor = new();
    private readonly SemaphoreSlim _activationGate = new(0, 1);
    private readonly SemaphoreSlim _completionGate = new(0, 1);

    private readonly Dictionary<string, OrganismWrapper> _organismsById = new();
    private readonly List<OrganismWrapper> _organismsList = new();

    private GameEngine? _currentEngine;
    private PrivateAssemblyCache? _pac;

    private OrganismWrapper? _currentBug;
    private CancellationTokenSource? _timeoutCts;
    private Task? _activationTask;
    private volatile bool _exitRequested;

    private int _organismsActivated;
    private int _orgEnumIndex;
    private int _tickCount;
    private long _totalActivations;
    private long _lastReport;
    private int _ticksToSuspendBlacklisting;
    private bool _penalizeForTime = true;
    private bool _disposed;

    public GameScheduler(ILogger<GameScheduler> logger)
    {
        _logger = logger;
        MaxAllowance = EngineSettings.OrganismSchedulingBlacklistOvertime;
        TicksPerSec = 5;
        Quantum = 5000;
        MaxOverage = EngineSettings.OrganismSchedulingMaximumOvertime;

        Debug.Assert(MaxOverage > Quantum);

        _activationTask = Task.Run(ActivationLoop);
    }

    // --- Properties ---

    /// <summary>Number of creatures run per tick.</summary>
    public int OrganismsPerTick => (_organismsList.Count / TicksPerSec) + 1;

    /// <summary>Ticks per second (buckets organisms are divided into).</summary>
    public int TicksPerSec { get; set; }

    /// <summary>Maximum microseconds allowed per organism turn.</summary>
    public int Quantum { get; set; }

    /// <summary>Maximum overtime before organism turn is skipped.</summary>
    public long MaxOverage { get; set; }

    /// <summary>Maximum overtime before organism is permanently removed (microseconds).</summary>
    public long MaxAllowance { get; set; }

    /// <summary>The current world state snapshot.</summary>
    public WorldState? CurrentState { get; set; }

    /// <summary>Whether to penalize organisms for exceeding time budgets.</summary>
    public bool PenalizeForTime
    {
        get => DetectDeadlock && _penalizeForTime;
        set => _penalizeForTime = value;
    }

    /// <summary>Suspend blacklisting entirely.</summary>
    public bool SuspendBlacklisting { get; set; }

    /// <summary>Whether deadlock detection is active.</summary>
    public bool DetectDeadlock
    {
        get
        {
            if (Debugger.IsAttached) return false;
            if (_ticksToSuspendBlacklisting > 0) return false;
            return !SuspendBlacklisting;
        }
    }

    /// <summary>All organisms currently in the scheduler.</summary>
    public IReadOnlyList<Organism> Organisms
    {
        get
        {
            var list = new List<Organism>(_organismsList.Count);
            foreach (var wrapper in _organismsList)
            {
                list.Add(wrapper.Organism);
            }
            return list;
        }
    }

    /// <summary>The number of loaded organisms.</summary>
    public int OrganismCount => _organismsList.Count;

    /// <summary>
    /// Sets the current game engine and wires up the assembly cache.
    /// </summary>
    public GameEngine? CurrentGameEngine
    {
        get => _currentEngine;
        set
        {
            _currentEngine = value;
            // TODO: Sprint 7 — wire up PAC from engine when available
        }
    }

    /// <summary>
    /// Sets the private assembly cache used for assembly resolution.
    /// </summary>
    public PrivateAssemblyCache? Pac
    {
        get => _pac;
        set
        {
            _pac?.Close();
            _pac = value;
            // TODO: Sprint 7 — hook assembly resolve via AssemblyLoadContext
        }
    }

    // --- Core scheduling ---

    /// <summary>
    /// Called by the game engine each tick to give a set of organisms time slices.
    /// </summary>
    public void Tick()
    {
        var activated = 0;

        if (_ticksToSuspendBlacklisting > 0)
        {
            _logger.LogDebug("Suspending blacklisting for this tick.");
        }

        if (_organismsActivated < _organismsList.Count)
        {
            while (activated < OrganismsPerTick && _orgEnumIndex < _organismsList.Count)
            {
                var wrapper = _organismsList[_orgEnumIndex];
                _orgEnumIndex++;
                activated++;

                RunOrganismWithDeadlockDetection(wrapper);
            }
        }

        _organismsActivated += activated;
        _tickCount++;

        if (_organismsActivated >= _organismsList.Count && _tickCount >= TicksPerSec)
        {
            _orgEnumIndex = 0;
            _organismsActivated = 0;
            _tickCount = 0;
            if (_ticksToSuspendBlacklisting > 0)
            {
                _ticksToSuspendBlacklisting--;
            }
        }
    }

    /// <summary>
    /// Adds an organism to the scheduler.
    /// </summary>
    public void Add(Organism org, string id)
    {
        Debug.Assert(_organismsActivated == 0);

        if (_organismsById.ContainsKey(id))
        {
            throw new InvalidOperationException($"Organism '{id}' already exists in the scheduler.");
        }

        var wrapper = new OrganismWrapper(org, id);
        _organismsById[id] = wrapper;
        _organismsList.Add(wrapper);
    }

    /// <summary>
    /// Retrieves an organism by ID.
    /// </summary>
    public Organism? GetOrganism(string id)
    {
        return _organismsById.TryGetValue(id, out var wrapper) ? wrapper.Organism : null;
    }

    /// <summary>
    /// Removes an organism from the scheduler.
    /// </summary>
    public void Remove(string organismID)
    {
        if (_organismsById.Remove(organismID, out var wrapper))
        {
            _organismsList.Remove(wrapper);
        }
    }

    /// <summary>
    /// Creates a new organism of the given species type and adds it.
    /// </summary>
    public void Create(Type species, string id)
    {
        var newOrganism = (Organism?)Activator.CreateInstance(species)
            ?? throw new InvalidOperationException($"Failed to create instance of: {species}");

        Add(newOrganism, id);
    }

    /// <summary>
    /// Gathers all pending actions from organisms.
    /// </summary>
    public TickActions GatherTickActions()
    {
        var act = new TickActions();
        act.GatherActionsFromOrganisms(Organisms);
        return act;
    }

    /// <summary>
    /// Temporarily suspends blacklisting for the next 2 ticks (e.g. power saving mode).
    /// </summary>
    public void TemporarilySuspendBlacklisting()
    {
        _ticksToSuspendBlacklisting = 2;
    }

    /// <summary>
    /// Returns a timing report for the specified organism.
    /// </summary>
    public string GetOrganismTimingReport(string organismID)
    {
        if (!_organismsById.TryGetValue(organismID, out var w))
        {
            return "[organism doesn't exist]";
        }

        if (!PenalizeForTime)
        {
            return $"Inaccurate time due to debugging: {w.LastTime} microseconds";
        }

        return w.LastTime > Quantum
            ? $"Warning: Time to execute last turn: {w.LastTime} microseconds is over maximum allowed time of {Quantum} microseconds. Animal may be penalized by skipping a turn."
            : $"Time to execute last turn: {w.LastTime} microseconds. [less than maximum allowed time of {Quantum} microseconds.]";
    }

    // --- Deadlock detection ---

    /// <summary>
    /// Runs an organism with deadlock detection using CancellationToken-based timeouts
    /// instead of Thread.Abort.
    /// </summary>
    private void RunOrganismWithDeadlockDetection(OrganismWrapper currentAnimal)
    {
        var sw = Stopwatch.StartNew();
        var tries = 0;
        var blacklist = false;
        var shutdownWithoutBlacklist = false;

        // Hand the activation loop an organism and kick off processing
        _currentBug = currentAnimal;
        _activationGate.Release();

        while (true)
        {
            var executionDone = _completionGate.Wait(DeadlockCheckIntervalMs);

            if (!executionDone)
            {
                _logger.LogWarning("Organism thread not stopped after {Interval} ms, checking elapsed time.",
                    DeadlockCheckIntervalMs);

                if (DetectDeadlock)
                {
                    if (PenalizeForTime)
                    {
                        var elapsedMs = sw.ElapsedMilliseconds;
                        if (elapsedMs > DeadlockThresholdMs)
                        {
                            _logger.LogError("Thread overtime: {Seconds:F2} seconds, blacklist and exit.",
                                elapsedMs / 1000.0);
                            blacklist = true;
                            break;
                        }
                    }

                    tries++;
                    if (tries >= DeadlockRetries)
                    {
                        _logger.LogWarning("Tried accessing organism thread {Tries} times, not blacklisted — restart.",
                            tries);
                        shutdownWithoutBlacklist = true;
                        break;
                    }
                }
                else
                {
                    _logger.LogDebug("Deadlock detection off.");
                }
            }
            else
            {
                break;
            }
        }

        HandleRestartsAndBlacklist(currentAnimal, blacklist);
        HandleRestartWithoutBlacklist(blacklist, shutdownWithoutBlacklist);
    }

    private static void HandleRestartWithoutBlacklist(bool blacklist, bool shutdownWithoutBlacklist)
    {
        if (shutdownWithoutBlacklist || blacklist)
        {
            throw new MaliciousOrganismException();
        }
    }

    private void HandleRestartsAndBlacklist(OrganismWrapper currentAnimal, bool blacklist)
    {
        if (DetectDeadlock && blacklist)
        {
            var speciesName = currentAnimal.Organism.State?.Species is Species s
                ? s.AssemblyFullName
                : currentAnimal.ID;

            _logger.LogError("Permanently blacklisting: {Species}", speciesName);

            if (_pac != null)
            {
                _pac.LastRun = speciesName;
            }

            throw new MaliciousOrganismException();
        }
    }

    // --- Activation loop (runs on background Task) ---

    /// <summary>
    /// Background loop that waits for organisms to be scheduled, then executes
    /// their think method with timeout protection via CancellationToken.
    /// </summary>
    private void ActivationLoop()
    {
        while (!_exitRequested)
        {
            _activationGate.Wait();
            if (_exitRequested) return;

            var bug = _currentBug!;
            var success = false;
            long duration = 0;
            var deathReason = PopulationChangeReason.NotDead;
            var exceptionInfo = "";
            var skippedTurn = false;

            if (bug.Active)
            {
                var state = CurrentState?.GetOrganismState(bug.Organism.ID);
                if (state == null || !state.IsAlive)
                {
                    bug.Active = false;
                }
                else
                {
                    using var cts = new CancellationTokenSource();
                    _timeoutCts = cts;

                    try
                    {
                        if (bug.Overage > MaxAllowance)
                        {
                            _logger.LogWarning("Organism blacklisted: overage {Overage} > max allowance {Max}.",
                                bug.Overage, MaxAllowance);
                            deathReason = PopulationChangeReason.Timeout;
                            bug.Active = false;
                        }
                        else if (bug.Overage > MaxOverage)
                        {
                            bug.Overage -= Quantum;
                            if (bug.Overage < 0) bug.Overage = 0;

                            bug.Organism.InternalMain(true);
                            bug.Organism.WriteTrace(
                                $"Animal's turn was skipped because they took longer than {Quantum} microseconds for their turn too many times.");

                            success = true;
                            skippedTurn = true;
                        }
                        else
                        {
                            cts.CancelAfter(TimeSpan.FromMilliseconds(1000));

                            _monitor.Start();

                            // Run organism code
                            bug.Organism.InternalMain(false);

                            duration = _monitor.EndGetMicroseconds();
                            success = true;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        deathReason = PopulationChangeReason.Timeout;
                        _logger.LogWarning("Organism timed out via cancellation.");
                    }
                    catch (Exception e) when (e is not MaliciousOrganismException)
                    {
                        deathReason = PopulationChangeReason.Error;
                        exceptionInfo = e.ToString();
                    }
                    finally
                    {
                        _timeoutCts = null;

                        if (!success)
                        {
                            if (_currentEngine != null)
                            {
                                _currentEngine.RemoveOrganismQueued(
                                    deathReason == PopulationChangeReason.Timeout
                                        ? new KilledOrganism(bug.Organism.ID, deathReason)
                                        : new KilledOrganism(bug.Organism.ID, deathReason, exceptionInfo));

                                if (deathReason != PopulationChangeReason.Timeout)
                                {
                                    _logger.LogWarning("Exception in organism: {Info}", exceptionInfo);
                                }
                            }
                        }
                        else
                        {
                            _totalActivations++;
                            if (PenalizeForTime)
                            {
                                bug.TotalTime += duration;
                                bug.LastTime = duration;
                                if (duration > Quantum)
                                {
                                    bug.Overage += (duration - Quantum);
                                }
                                else if (bug.Overage > 0 && !skippedTurn)
                                {
                                    bug.Overage -= (Quantum - duration);
                                    if (bug.Overage < 0) bug.Overage = 0;
                                }
                            }
                            else
                            {
                                bug.LastTime = duration;
                                bug.Overage = 0;
                            }

                            bug.TotalActivations++;
                        }

                        if (_totalActivations > _lastReport + ReportInterval)
                        {
                            _lastReport = _totalActivations;
                        }
                    }
                }
            }

            // Signal that this organism is done
            try { _completionGate.Release(); }
            catch (SemaphoreFullException) { /* already signaled */ }
        }
    }

    /// <summary>
    /// Shuts down the scheduler and releases resources.
    /// </summary>
    public void Close()
    {
        _exitRequested = true;
        try { _activationGate.Release(); }
        catch (SemaphoreFullException) { }

        _activationTask?.Wait(TimeSpan.FromSeconds(5));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Close();
        _activationGate.Dispose();
        _completionGate.Dispose();
        _timeoutCts?.Dispose();
    }

    // --- Inner types ---

    /// <summary>
    /// Wraps an organism with timing and status metadata.
    /// </summary>
    internal sealed class OrganismWrapper
    {
        public OrganismWrapper(Organism organism, string id)
        {
            Organism = organism;
            ID = id;
        }

        public Organism Organism { get; }
        public string ID { get; }
        public bool Active { get; set; } = true;
        public long Overage { get; set; }
        public long TotalTime { get; set; }
        public long LastTime { get; set; }
        public long TotalActivations { get; set; }
    }

    /// <summary>
    /// Exception thrown when a malicious or deadlocked organism is detected.
    /// </summary>
    public class MaliciousOrganismException : Exception
    {
        public MaliciousOrganismException() : base("A malicious organism was detected.") { }
        public MaliciousOrganismException(string message) : base(message) { }
        public MaliciousOrganismException(string message, Exception inner) : base(message, inner) { }
    }
}
