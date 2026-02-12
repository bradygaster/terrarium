// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Drawing;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OrganismBase;
using Terrarium.Configuration;
using Terrarium.Game.Networking;
using Terrarium.Game.Rendering;
using Terrarium.Game.Services;
using Terrarium.Net;

namespace Terrarium.Game;

/// <summary>
/// The core Terrarium game engine. Controls creatures, updates world data,
/// and manages all game events. The 10-phase ProcessTurn() method is the heart
/// of the game loop.
/// </summary>
public class GameEngine : IGameEngine
{
    private readonly ILogger<GameEngine> _logger;
    private readonly Random _random = new(DateTime.Now.Millisecond);
    private readonly ConcurrentQueue<object> _newOrganismQueue = new();
    private readonly Queue<KilledOrganism> _removeOrganismQueue = new();
    private GameRenderBridge? _renderBridge;
    private GameNetworkBridge? _networkBridge;
    private GameServiceBridge? _serviceBridge;
    private IGameStatePersistence? _statePersistence;

    private int _maxAnimals = 50;
    private int _maxPlants = 50;
    private int _worldHeight = 2048;
    private int _worldWidth = 2048;
    private int _animalCount;
    private int _plantCount;
    private int _turnPhase;

    // Performance metrics (System.Diagnostics.Metrics)
    private static readonly Meter s_meter = new("Terrarium.Game.GameEngine", "1.0.0");
    private static readonly Histogram<double> s_processTurnDuration = s_meter.CreateHistogram<double>(
        "game_engine.process_turn.duration",
        unit: "ms",
        description: "Duration of complete ProcessTurn (10 phases) in milliseconds");
    private static readonly Histogram<double> s_phaseDuration = s_meter.CreateHistogram<double>(
        "game_engine.phase.duration",
        unit: "ms",
        description: "Duration of individual turn phases in milliseconds");
    private static readonly Counter<long> s_ticksCompleted = s_meter.CreateCounter<long>(
        "game_engine.ticks.completed",
        unit: "ticks",
        description: "Total number of completed game ticks");
    private static readonly ObservableGauge<int> s_organismCount = s_meter.CreateObservableGauge<int>(
        "game_engine.organisms.count",
        () => new[] {
            new Measurement<int>(_staticAnimalCount, new KeyValuePair<string, object?>("type", "animal")),
            new Measurement<int>(_staticPlantCount, new KeyValuePair<string, object?>("type", "plant"))
        },
        unit: "organisms",
        description: "Current organism count by type");

    private readonly Stopwatch _tickStopwatch = new();
    private readonly Stopwatch _phaseStopwatch = new();
    private static int _staticAnimalCount;
    private static int _staticPlantCount;

    private WorldVector? _currentVector;
    private WorldState? _newWorldState;
    private string[]? _organismIDList;
    private PopulationData? _populationData;
    private bool _ecosystemMode;
    private EcosystemMode _mode;

    /// <summary>
    /// Creates a new headless GameEngine.
    /// </summary>
    public GameEngine(
        ILogger<GameEngine> logger,
        PopulationData populationData,
        int worldWidth = 2048,
        int worldHeight = 2048,
        int maxAnimals = 50,
        int maxPlants = 50,
        bool ecosystemMode = false)
    {
        _logger = logger;
        _populationData = populationData;
        _worldWidth = worldWidth;
        _worldHeight = worldHeight;
        _maxAnimals = maxAnimals;
        _maxPlants = maxPlants;
        _ecosystemMode = ecosystemMode;
        _mode = ecosystemMode ? Game.EcosystemMode.Networked : Game.EcosystemMode.LocalOnly;

        EngineSettings.EngineSettingsAsserts();

        // Normalize to grid cell boundaries
        if (_worldWidth % EngineSettings.GridCellWidth != 0)
            _worldWidth += EngineSettings.GridCellWidth - (_worldWidth % EngineSettings.GridCellWidth);
        if (_worldHeight % EngineSettings.GridCellHeight != 0)
            _worldHeight += EngineSettings.GridCellHeight - (_worldHeight % EngineSettings.GridCellHeight);

        // Set up initial world state
        var currentState = new WorldState(GridWidth, GridHeight);
        currentState.TickNumber = 0;
        currentState.StateGuid = Guid.NewGuid();
        currentState.Teleporter = new Teleporter(_maxAnimals / EngineSettings.NumberOfAnimalsPerTeleporter);
        currentState.MakeImmutable();

        _currentVector = new WorldVector(currentState);

        // TODO: Sprint 7 — create scheduler and set CurrentState
        _logger.LogInformation("GameEngine initialized: {Width}x{Height}, max animals={Animals}, max plants={Plants}",
            _worldWidth, _worldHeight, _maxAnimals, _maxPlants);
    }

    /// <summary>World height in pixels.</summary>
    public int WorldHeight => _worldHeight;

    /// <summary>World width in pixels.</summary>
    public int WorldWidth => _worldWidth;

    /// <summary>World width in grid cells.</summary>
    public int GridWidth => _worldWidth >> EngineSettings.GridWidthPowerOfTwo;

    /// <summary>World height in grid cells.</summary>
    public int GridHeight => _worldHeight >> EngineSettings.GridHeightPowerOfTwo;

    /// <summary>Max animal count.</summary>
    public int MaxAnimals => _maxAnimals;

    /// <summary>Max plant count.</summary>
    public int MaxPlants => _maxPlants;

    /// <summary>Current animal count.</summary>
    public int AnimalCount => _animalCount;

    /// <summary>Current plant count.</summary>
    public int PlantCount => _plantCount;

    /// <summary>Current turn phase (0-9).</summary>
    public int TurnPhase => _turnPhase;

    /// <summary>Whether in ecosystem mode.</summary>
    public bool EcosystemMode => _ecosystemMode;

    /// <summary>Gets or sets the ecosystem networking mode.</summary>
    public Game.EcosystemMode Mode
    {
        get => _mode;
        set
        {
            if (_mode != value)
            {
                _mode = value;
                _logger.LogInformation("Ecosystem mode changed to: {Mode}", _mode);
                
                // Propagate mode changes to PopulationData
                if (_populationData is not null)
                {
                    _populationData.Mode = _mode;
                }
            }
        }
    }

    /// <summary>Population data tracker.</summary>
    public PopulationData? PopulationData => _populationData;

    /// <summary>Sets the render bridge for tick-based rendering.</summary>
    public GameRenderBridge? RenderBridge
    {
        get => _renderBridge;
        set => _renderBridge = value;
    }

    /// <summary>Sets the network bridge for SignalR-based P2P networking.</summary>
    public GameNetworkBridge? NetworkBridge
    {
        get => _networkBridge;
        set => _networkBridge = value;
    }

    /// <summary>Sets the service bridge for server HTTP communication.</summary>
    public GameServiceBridge? ServiceBridge
    {
        get => _serviceBridge;
        set
        {
            _serviceBridge = value;
            if (_populationData is not null)
                _populationData.ServiceBridge = value;
        }
    }

    /// <summary>Sets the game state persistence handler for save/load operations.</summary>
    public IGameStatePersistence? StatePersistence
    {
        get => _statePersistence;
        set => _statePersistence = value;
    }

    /// <summary>The current world vector (state + actions).</summary>
    public WorldVector? CurrentVector
    {
        get => _currentVector;
        set
        {
            var oldVector = _currentVector;
            _currentVector = value;
            WorldVectorChanged?.Invoke(this, new WorldVectorChangedEventArgs(oldVector, _currentVector));
        }
    }

    /// <summary>Fired when the world vector changes.</summary>
    public event EventHandler<WorldVectorChangedEventArgs>? WorldVectorChanged;

    /// <summary>Fired when engine state changes (for UI notifications).</summary>
    public event EventHandler<EngineStateChangedEventArgs>? EngineStateChanged;

    /// <summary>
    /// Processes turns in a phase manner. After 10 calls, one game tick is complete.
    /// </summary>
    /// <returns>True if a complete tick has been processed.</returns>
    public bool ProcessTurn()
    {
        // Start timing if this is phase 0 (start of new tick)
        if (_turnPhase == 0)
        {
            _tickStopwatch.Restart();
        }

        _phaseStopwatch.Restart();

        // The 10-phase processing loop. Each phase is designed to take roughly
        // equal time. The screen can be painted between phases for smooth frame rate.
        // After all 10 phases, one game "tick" is complete.
        switch (_turnPhase)
        {
            case 0:
                // Phase 0: Save state if needed, give 1/5 of organisms time
                if (_ecosystemMode && _populationData != null &&
                    _populationData.IsReportingTick(CurrentVector!.State.TickNumber - 1))
                {
                    // TODO: Sprint 7 — serialize state via JSON
                    _logger.LogDebug("State save point at tick {Tick}", CurrentVector.State.TickNumber);
                }
                // TODO: Sprint 7 — Scheduler.Tick()
                break;

            case 1:
                // Phase 1: Give 1/5 of organisms time
                // TODO: Sprint 7 — Scheduler.Tick()
                break;

            case 2:
                // Phase 2: Give 1/5 of organisms time
                // TODO: Sprint 7 — Scheduler.Tick()
                break;

            case 3:
                // Phase 3: Give 1/5 of organisms time
                // TODO: Sprint 7 — Scheduler.Tick()
                break;

            case 4:
                // Phase 4: Give 1/5 of organisms time
                // TODO: Sprint 7 — Scheduler.Tick()
                break;

            case 5:
                // Phase 5: Gather actions, create mutable next state, remove queued organisms
                var act = new TickActions();
                // TODO: Sprint 7 — act.GatherActionsFromOrganisms(scheduler.Organisms)
                CurrentVector!.Actions = act;

                _newWorldState = CurrentVector.State.DuplicateMutable();
                _newWorldState.TickNumber = _newWorldState.TickNumber + 1;
                _populationData?.BeginTick(_newWorldState.TickNumber, CurrentVector.State.StateGuid);

                RemoveOrganismsFromQueue();

                _organismIDList = _newWorldState.OrganismIDs.ToArray();
                KillDiseasedOrganisms();
                break;

            case 6:
                // Phase 6: Burn energy, attacks, defends, movement vectors
                Debug.Assert(_newWorldState != null, "Worldstate did not get created for this tick");
                BurnBaseEnergy();
                DoAttacks();
                DoDefends();
                ChangeMovementVectors();
                break;

            case 7:
                // Phase 7: Move animals
                MoveAnimals();
                break;

            case 8:
                // Phase 8: Bites, growth, incubation, reproduction, healing
                DoBites();
                GrowAllOrganisms();
                Incubate();
                StartReproduction();
                Heal();
                break;

            case 9:
                // Phase 9: Plant energy, teleportation, insertions, antennas, finalize
                GiveEnergyToPlants();
                TeleportOrganisms();
                InsertOrganismsFromQueue();
                DoAntennas();

                _newWorldState!.MakeImmutable();
                var vector = new WorldVector(_newWorldState);
                CurrentVector = vector;

                // TODO: Sprint 7 — scheduler.CurrentState = _newWorldState
                _populationData?.EndTick(_newWorldState.TickNumber);
                _newWorldState = null;
                break;
        }

        _phaseStopwatch.Stop();
        s_phaseDuration.Record(_phaseStopwatch.Elapsed.TotalMilliseconds, 
            new KeyValuePair<string, object?>("phase", _turnPhase));

        _turnPhase++;
        if (_turnPhase == 10)
        {
            _turnPhase = 0;

            // Record tick metrics
            _tickStopwatch.Stop();
            s_processTurnDuration.Record(_tickStopwatch.Elapsed.TotalMilliseconds);
            s_ticksCompleted.Add(1);
            _staticAnimalCount = _animalCount;
            _staticPlantCount = _plantCount;

            // Dispatch render after each completed tick
            if (_renderBridge is not null)
            {
                // Fire-and-forget: rendering should not block the game loop
                _ = _renderBridge.RenderTickAsync(this);
            }

            return true;
        }
        return false;
    }

    /// <summary>
    /// Queues an organism for removal at the next safe point.
    /// </summary>
    public void RemoveOrganismQueued(KilledOrganism killedOrganism)
    {
        _removeOrganismQueue.Enqueue(killedOrganism);
    }

    /// <summary>
    /// Adds a new organism to the insertion queue.
    /// </summary>
    public void AddNewOrganism(Species species, Point preferredLocation)
    {
        NewOrganism newOrganism;
        if (preferredLocation == Point.Empty)
        {
            newOrganism = new NewOrganism(species.InitializeNewState(new Point(0, 0), 0), null);
        }
        else
        {
            newOrganism = new NewOrganism(species.InitializeNewState(preferredLocation, 0), null);
            newOrganism.AddAtRandomLocation = false;
        }
        _newOrganismQueue.Enqueue(newOrganism);
    }

    /// <summary>
    /// Introduces a creature by loading its assembly from the private assembly cache,
    /// validating it, extracting species information, and adding it to the game.
    /// </summary>
    /// <param name="assemblyFullName">The full name of the assembly to load.</param>
    /// <param name="pac">The private assembly cache containing creature assemblies.</param>
    /// <param name="validator">Optional assembly validator.</param>
    /// <param name="preferredLocation">Optional preferred location for the new organism.</param>
    /// <returns>True if the creature was successfully introduced; false otherwise.</returns>
    public bool IntroduceCreatureFromPac(
        string assemblyFullName,
        Hosting.PrivateAssemblyCache pac,
        Hosting.AssemblyValidator? validator = null,
        Point? preferredLocation = null)
    {
        try
        {
            // Check if assembly exists in PAC
            if (!pac.Exists(assemblyFullName))
            {
                _logger.LogWarning("Assembly not found in PAC: {AssemblyFullName}", assemblyFullName);
                return false;
            }

            // Validate the assembly if validator is provided
            if (validator != null)
            {
                var assemblyPath = pac.GetFileName(assemblyFullName);
                var validationResult = validator.Validate(assemblyPath);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Assembly validation failed: {Reasons}",
                        string.Join("; ", validationResult.Reasons));
                    return false;
                }
            }

            // Load the assembly
            var assembly = pac.LoadOrganismAssembly(assemblyFullName);

            // Extract species information
            var species = Species.GetSpeciesFromAssembly(assembly);

            // Add the organism to the game
            AddNewOrganism(species, preferredLocation ?? Point.Empty);

            _logger.LogInformation("Successfully introduced creature: {SpeciesName} ({AssemblyFullName})",
                species.Name, assemblyFullName);

            // Register the species with the server if service bridge is available
            if (_serviceBridge != null)
            {
                var assemblyBytes = File.ReadAllBytes(pac.GetFileName(assemblyFullName));
                _ = _serviceBridge.RegisterSpeciesAsync(species, assemblyBytes);
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to introduce creature from PAC: {AssemblyFullName}", assemblyFullName);
            return false;
        }
    }

    /// <summary>
    /// Downloads a creature assembly from the server and introduces it into the local ecosystem.
    /// </summary>
    /// <param name="speciesName">The name of the species to download.</param>
    /// <param name="version">The version of the species.</param>
    /// <param name="pac">The private assembly cache to save the downloaded assembly to.</param>
    /// <param name="validator">Optional assembly validator.</param>
    /// <param name="preferredLocation">Optional preferred location for the new organism.</param>
    /// <returns>True if the creature was successfully downloaded and introduced; false otherwise.</returns>
    public async Task<bool> IntroduceCreatureFromServerAsync(
        string speciesName,
        string version,
        Hosting.PrivateAssemblyCache pac,
        Hosting.AssemblyValidator? validator = null,
        Point? preferredLocation = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_serviceBridge == null)
            {
                _logger.LogWarning("Cannot download creature: ServiceBridge is not configured");
                return false;
            }

            // Download the assembly bytes from the server
            var assemblyBytes = await _serviceBridge.GetSpeciesAssemblyAsync(speciesName, version, cancellationToken);
            if (assemblyBytes == null || assemblyBytes.Length == 0)
            {
                _logger.LogWarning("Downloaded assembly is empty for species: {SpeciesName}", speciesName);
                return false;
            }

            // Save to a temporary file for validation
            var tempPath = Hosting.PrivateAssemblyCache.GetSafeTempFileName();
            try
            {
                await File.WriteAllBytesAsync(tempPath, assemblyBytes, cancellationToken);

                // Validate the assembly if validator is provided
                if (validator != null)
                {
                    var validationResult = validator.Validate(tempPath);
                    if (!validationResult.IsValid)
                    {
                        _logger.LogWarning("Downloaded assembly validation failed for {SpeciesName}: {Reasons}",
                            speciesName, string.Join("; ", validationResult.Reasons));
                        return false;
                    }
                }

                // Load the assembly to extract full name and species info
                using var ms = new System.IO.MemoryStream(assemblyBytes);
                var assembly = System.Reflection.Assembly.Load(assemblyBytes);
                var species = Species.GetSpeciesFromAssembly(assembly);
                var assemblyFullName = assembly.FullName!;

                // Save to PAC
                await pac.SaveOrganismBytesAsync(assemblyBytes, assemblyFullName, cancellationToken);

                // Introduce the creature
                AddNewOrganism(species, preferredLocation ?? Point.Empty);

                _logger.LogInformation("Successfully downloaded and introduced creature: {SpeciesName} ({AssemblyFullName})",
                    species.Name, assemblyFullName);

                return true;
            }
            finally
            {
                // Clean up temp file
                if (File.Exists(tempPath))
                {
                    File.Delete(tempPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download and introduce creature from server: {SpeciesName}", speciesName);
            return false;
        }
    }

    /// <summary>
    /// Stops the game and optionally serializes state.
    /// </summary>
    public void StopGame(bool serializeState)
    {
        if (serializeState)
        {
            // TODO: Sprint 7 — serialize state via System.Text.Json
            _logger.LogInformation("Game state serialization requested");
        }

        _populationData?.Close();
        _populationData = null;

        // TODO: Sprint 7 — destroy scheduler
        _logger.LogInformation("Game stopped");
    }

    #region Phase helper methods

    private void ChangeMovementVectors()
    {
        foreach (var action in CurrentVector!.Actions.MoveToActions.Values)
        {
            var state = _newWorldState!.GetOrganismState(action.OrganismID);
            if (state != null && state.IsAlive)
                state.CurrentMoveToAction = action;
        }
    }

    private void DoAntennas()
    {
        foreach (var orgState in _newWorldState!.Organisms)
        {
            if (orgState is not AnimalState animalState) continue;
            // TODO: Sprint 7 — get Animal from scheduler, read antennas
            var antennaState = new AntennaState((AntennaState?)null);
            antennaState.MakeImmutable();
            animalState.Antennas = antennaState;
        }
    }

    private void DoDefends()
    {
        foreach (var action in CurrentVector!.Actions.DefendActions.Values)
        {
            var defenderState = _newWorldState!.GetOrganismState(action.OrganismID) as AnimalState;
            if (defenderState != null && defenderState.IsAlive)
                defenderState.OrganismEvents!.DefendCompleted = new DefendCompletedEventArgs(action.ActionID, action);
        }
    }

    private void DoAttacks()
    {
        foreach (var action in CurrentVector!.Actions.AttackActions.Values)
        {
            var attackerState = _newWorldState!.GetOrganismState(action.OrganismID) as AnimalState;
            if (attackerState == null || !attackerState.IsAlive) continue;

            var defenderState = _newWorldState.GetOrganismState(action.TargetAnimal.ID) as AnimalState;
            int damageCaused = 0;
            bool escaped = false;
            bool killed = false;

            if (defenderState == null)
            {
                escaped = true;
            }
            else if (attackerState.IsWithinRect(1, defenderState))
            {
                if (defenderState.IsAlive)
                {
                    damageCaused = _random.Next(0, attackerState.AnimalSpecies.MaximumAttackDamagePerUnitRadius * attackerState.Radius);

                    if (CurrentVector.Actions.DefendActions.TryGetValue(defenderState.ID, out var defendAction) &&
                        defendAction.TargetAnimal.ID == attackerState.ID)
                    {
                        var defendDiscount = _random.Next(0,
                            defenderState.AnimalSpecies.MaximumDefendDamagePerUnitRadius * defenderState.Radius);
                        damageCaused = Math.Max(0, damageCaused - defendDiscount);
                    }

                    if (damageCaused > 0) defenderState.CauseDamage(damageCaused);
                    killed = !defenderState.IsAlive;
                    defenderState.OrganismEvents!.AttackedEvents.Add(new AttackedEventArgs(attackerState));
                }
            }

            attackerState.OrganismEvents!.AttackCompleted =
                new AttackCompletedEventArgs(action.ActionID, action, killed, escaped, damageCaused);
        }
    }

    private void MoveAnimals()
    {
        _newWorldState!.Teleporter?.Move(WorldWidth, WorldHeight);

        var index = new GridIndex();
        foreach (var organismID in _organismIDList!)
        {
            var organismState = _newWorldState.GetOrganismState(organismID);
            if (organismState == null) continue;

            if (organismState is AnimalState animalState)
            {
                if (!animalState.IsStopped)
                {
                    var vector = Vector.Subtract(animalState.Position,
                        animalState.CurrentMoveToAction!.MovementVector.Destination);
                    Point newLocation;
                    if (vector.Magnitude <= animalState.CurrentMoveToAction.MovementVector.Speed)
                        newLocation = animalState.CurrentMoveToAction.MovementVector.Destination;
                    else
                    {
                        var unitVector = vector.GetUnitVector();
                        var speedVector = unitVector.Scale(animalState.CurrentMoveToAction.MovementVector.Speed);
                        newLocation = Vector.Add(animalState.Position, speedVector);
                    }
                    index.AddPath(animalState, animalState.Position, newLocation, GridWidth, GridHeight);
                }
                else
                    index.AddPath(animalState, animalState.Position, animalState.Position, GridWidth, GridHeight);
            }
            else
                index.AddPath(organismState, organismState.Position, organismState.Position, GridWidth, GridHeight);
        }

        index.ResolvePaths();
        _newWorldState.ClearIndex();

        foreach (var segment in index.StartSegments)
        {
            if (segment.IsStationarySegment)
            {
                var stState = segment.State;
                if (stState.CurrentMoveToAction != null)
                {
                    stState.OrganismEvents!.MoveCompleted =
                        new MoveCompletedEventArgs(stState.CurrentMoveToAction.ActionID,
                            stState.CurrentMoveToAction, ReasonForStop.DestinationReached, null);
                    stState.CurrentMoveToAction = null;
                }
                continue;
            }

            var endSegment = segment;
            while (endSegment.Next != null) endSegment = endSegment.Next;

            var newState = (AnimalState)endSegment.State;
            var moveVector = Vector.Subtract(endSegment.EndingPoint, endSegment.State.Position);
            newState.Position = endSegment.EndingPoint;

            Debug.Assert(endSegment.GridX == newState.GridX && endSegment.GridY == newState.GridY);
            newState.BurnEnergy(newState.EnergyRequiredToMove(moveVector.Magnitude,
                newState.CurrentMoveToAction!.MovementVector.Speed));

            if (!newState.IsAlive) continue;

            if (endSegment.ExitTime != 0)
            {
                newState.OrganismEvents!.MoveCompleted =
                    new MoveCompletedEventArgs(newState.CurrentMoveToAction.ActionID,
                        newState.CurrentMoveToAction, ReasonForStop.Blocked, endSegment.BlockedByState);
                newState.CurrentMoveToAction = null;
            }
            else if (endSegment.State.CurrentMoveToAction!.MovementVector.Destination == newState.Position)
            {
                newState.OrganismEvents!.MoveCompleted =
                    new MoveCompletedEventArgs(newState.CurrentMoveToAction.ActionID,
                        newState.CurrentMoveToAction, ReasonForStop.DestinationReached, null);
                newState.CurrentMoveToAction = null;
            }
        }

        _newWorldState.BuildIndex();
    }

    private void GiveEnergyToPlants()
    {
        foreach (var organismID in _organismIDList!)
        {
            var organismState = _newWorldState!.GetOrganismState(organismID);
            if (organismState is PlantState plantState && plantState.IsAlive)
            {
                var availableLight = CurrentVector!.State.GetAvailableLight(plantState);
                plantState.GiveEnergy(availableLight);
            }
        }
    }

    private void TeleportOrganisms()
    {
        // Skip teleportation in local-only mode
        if (_mode == Game.EcosystemMode.LocalOnly)
        {
            return;
        }

        foreach (var organismID in _organismIDList!)
        {
            var organismState = _newWorldState!.GetOrganismState(organismID);
            if (organismState == null) continue;
            if (organismState is AnimalState && !organismState.IsAlive) continue;
            if (_newWorldState.Teleporter != null && _newWorldState.Teleporter.IsInTeleporter(organismState))
            {
                if (_networkBridge is not null && _networkBridge.IsConnected)
                {
                    _networkBridge.SendTeleport(organismState);
                }
                _logger.LogDebug("Organism {ID} in teleporter zone", organismID);
            }
        }
    }

    private void BurnBaseEnergy()
    {
        foreach (var organismID in _organismIDList!)
        {
            var state = _newWorldState!.GetOrganismState(organismID);
            if (state == null || !state.IsAlive) continue;

            if (state is AnimalState)
                state.BurnEnergy(EngineSettings.BaseAnimalEnergyPerUnitOfRadius * state.Radius);
            else
                state.BurnEnergy(EngineSettings.BasePlantEnergyPerUnitOfRadius * state.Radius);
        }
    }

    private void DoBites()
    {
        foreach (var action in CurrentVector!.Actions.EatActions.Values)
        {
            var eaterState = _newWorldState!.GetOrganismState(action.OrganismID) as AnimalState;
            if (eaterState == null || !eaterState.IsAlive) continue;
            // TODO: Sprint 7 — implement bite/eat logic
        }
    }

    private void GrowAllOrganisms()
    {
        foreach (var organismID in _organismIDList!)
        {
            var state = _newWorldState!.GetOrganismState(organismID);
            if (state == null || !state.IsAlive) continue;

            var grownState = state.Grow();
            if (grownState != state && _newWorldState.OnlyOverlapsSelf(grownState))
                _newWorldState.RefreshOrganism(grownState);
        }
    }

    private void Incubate()
    {
        foreach (var organismID in _organismIDList!)
        {
            var state = _newWorldState!.GetOrganismState(organismID);
            if (state == null || !state.IsAlive || !state.IsIncubating) continue;

            if (state.IncubationTicks == EngineSettings.TicksToIncubate)
            {
                var newPosition = FindEmptyPosition(state.CellRadius, Point.Empty);
                if (newPosition != Point.Empty)
                {
                    var newOrganism = new NewOrganism(
                        ((Species)state.Species).InitializeNewState(newPosition, state.Generation + 1),
                        state.CurrentReproduceAction?.Dna != null
                            ? (byte[])state.CurrentReproduceAction.Dna.Clone()
                            : Array.Empty<byte>());
                    _newOrganismQueue.Enqueue(newOrganism);
                }

                state.OrganismEvents!.ReproduceCompleted =
                    new ReproduceCompletedEventArgs(state.CurrentReproduceAction!.ActionID,
                        state.CurrentReproduceAction);
                state.ResetReproductionWait();
                state.CurrentReproduceAction = null;
            }
            else if (state.EnergyState >= EnergyState.Normal)
            {
                if (state is AnimalState)
                    state.BurnEnergy(state.Radius * EngineSettings.AnimalIncubationEnergyPerUnitOfRadius);
                else
                    state.BurnEnergy(state.Radius * EngineSettings.PlantIncubationEnergyPerUnitOfRadius);
                state.AddIncubationTick();
            }
        }
    }

    private void StartReproduction()
    {
        foreach (var action in CurrentVector!.Actions.ReproduceActions.Values)
        {
            var state = _newWorldState!.GetOrganismState(action.OrganismID);
            if (state == null || !state.IsAlive) continue;
            // TODO: Sprint 7 — implement reproduction logic
        }
    }

    private void Heal()
    {
        foreach (var organismID in _organismIDList!)
        {
            var state = _newWorldState!.GetOrganismState(organismID);
            if (state == null || !state.IsAlive) continue;
            state.HealDamage();
        }
    }

    private void KillDiseasedOrganisms()
    {
        if (_plantCount <= _maxPlants && _animalCount <= _maxAnimals) return;

        // Kill excess organisms - oldest first for population control
        foreach (var organismID in _organismIDList!)
        {
            var state = _newWorldState!.GetOrganismState(organismID);
            if (state == null) continue;

            if (state is PlantState && _plantCount > _maxPlants)
            {
                RemoveOrganism(new KilledOrganism(state.ID, PopulationChangeReason.Sick));
                continue;
            }

            if (state is AnimalState && state.IsAlive && _animalCount > _maxAnimals)
            {
                state.Kill(PopulationChangeReason.Sick);
            }
        }
    }

    private void InsertOrganismsFromQueue()
    {
        while (_newOrganismQueue.TryDequeue(out var queueObject))
        {
            if (queueObject is NewOrganism newOrganism)
            {
                var organismState = newOrganism.State;
                var newPosition = FindEmptyPosition(organismState.CellRadius,
                    newOrganism.AddAtRandomLocation ? Point.Empty :
                        new Point(organismState.Position.X >> EngineSettings.GridWidthPowerOfTwo,
                            organismState.Position.Y >> EngineSettings.GridHeightPowerOfTwo));

                if (newPosition != Point.Empty)
                {
                    organismState.Position = newPosition;
                }
                else
                {
                    OnEngineStateChanged(new EngineStateChangedEventArgs(
                        $"A '{((Species)organismState.Species).Name}' died at birth: not enough space."));
                    continue;
                }

                // TODO: Sprint 7 — scheduler.Create(species.Type, organismState.ID)
                _newWorldState!.AddOrganism(organismState);
                organismState.OrganismEvents!.Born = new BornEventArgs(newOrganism.Dna);
                CountOrganism(organismState, PopulationChangeReason.Born);
            }
            // TODO: Sprint 7 — handle TeleportState objects
            if (queueObject is CreatureTeleport teleport)
            {
                _logger.LogDebug("Processing inbound teleport {TeleportId}", teleport.TeleportId);
                // Teleported creatures will be fully materialized when scheduler/assembly loading is wired
            }
        }
    }

    private void RemoveOrganismsFromQueue()
    {
        while (_removeOrganismQueue.Count > 0)
            RemoveOrganism(_removeOrganismQueue.Dequeue());
    }

    private void RemoveOrganism(KilledOrganism killedOrganism)
    {
        var killedState = _newWorldState!.GetOrganismState(killedOrganism.ID);
        if (killedState == null) return;

        if (killedOrganism.DeathReason == PopulationChangeReason.Error &&
            string.IsNullOrEmpty(killedOrganism.ExtraInformation))
        {
            killedState.Kill(killedOrganism.DeathReason);
        }
        else
        {
            UncountOrganism(killedState, killedOrganism.DeathReason);
            // TODO: Sprint 7 — Scheduler.Remove(killedOrganism.ID)
            _newWorldState.RemoveOrganism(killedOrganism.ID);
        }
    }

    private void CountOrganism(OrganismState state, PopulationChangeReason reason)
    {
        Debug.Assert(state != null);
        _populationData?.CountOrganism(((Species)state.Species).Name, reason);
        if (state is AnimalState) _animalCount++;
        else _plantCount++;
    }

    private void UncountOrganism(OrganismState state, PopulationChangeReason reason)
    {
        Debug.Assert(state != null);
        _populationData?.UncountOrganism(((Species)state.Species).Name, reason);
        if (state is AnimalState) _animalCount--;
        else _plantCount--;
    }

    private Point FindEmptyPosition(int cellRadius, Point preferredGridPoint)
    {
        var newLocation = preferredGridPoint == Point.Empty
            ? new Point(_random.Next(cellRadius, GridWidth - 1 - cellRadius),
                _random.Next(cellRadius, GridHeight - 1 - cellRadius))
            : preferredGridPoint;

        var retry = 20;
        while (retry > 0 &&
            _newWorldState!.FindOrganismsInCells(
                newLocation.X - cellRadius, newLocation.X + cellRadius,
                newLocation.Y - cellRadius, newLocation.Y + cellRadius).Count != 0)
        {
            newLocation = new Point(
                _random.Next(cellRadius, GridWidth - 1 - cellRadius),
                _random.Next(cellRadius, GridHeight - 1 - cellRadius));
            retry--;
        }

        return retry == 0
            ? Point.Empty
            : new Point(newLocation.X << EngineSettings.GridWidthPowerOfTwo,
                newLocation.Y << EngineSettings.GridHeightPowerOfTwo);
    }

    /// <summary>
    /// Queues a teleported creature received from the network for insertion.
    /// </summary>
    internal void OnTeleportReceived(CreatureTeleport teleport)
    {
        _newOrganismQueue.Enqueue(teleport);
        _logger.LogDebug("Queued inbound teleport {Id} for insertion", teleport.TeleportId);
    }

    /// <summary>
    /// Notifies listeners of an engine state change.
    /// </summary>
    internal void OnEngineStateChanged(EngineStateChangedEventArgs e)
    {
        _logger.LogDebug("Engine state changed: {Message}", e.Message);
        EngineStateChanged?.Invoke(this, e);
    }

    /// <summary>
    /// Saves the current game state to JSON via the StatePersistence handler.
    /// </summary>
    public async Task<string> SaveGameStateAsync(CancellationToken cancellationToken = default)
    {
        if (_statePersistence is null)
        {
            throw new InvalidOperationException("StatePersistence handler is not configured.");
        }

        var currentState = CurrentVector?.State;
        if (currentState is null)
        {
            throw new InvalidOperationException("No world state available to save.");
        }

        _logger.LogInformation("Saving game state at tick {Tick}", currentState.TickNumber);
        var json = await _statePersistence.SerializeWorldStateAsync(currentState, cancellationToken);
        return json;
    }

    /// <summary>
    /// Loads a saved game state from JSON and restores the world.
    /// </summary>
    public async Task LoadGameStateAsync(
        string json,
        Hosting.PrivateAssemblyCache pac,
        CancellationToken cancellationToken = default)
    {
        if (_statePersistence is null)
        {
            throw new InvalidOperationException("StatePersistence handler is not configured.");
        }

        _logger.LogInformation("Loading game state from JSON ({Length} bytes)", json.Length);
        var restoredState = await _statePersistence.DeserializeWorldStateAsync(json, cancellationToken);
        
        _logger.LogInformation("Restored state at tick {Tick} with {Count} organisms",
            restoredState.TickNumber, restoredState.Organisms.Count);

        // Replace current world state
        var vector = new WorldVector(restoredState);
        CurrentVector = vector;
        
        // Reset turn phase to start of tick
        _turnPhase = 0;
        
        // Update organism counts
        _animalCount = 0;
        _plantCount = 0;
        foreach (var organism in restoredState.Organisms)
        {
            if (organism is AnimalState) _animalCount++;
            else _plantCount++;
        }
        
        _logger.LogInformation("Game state loaded successfully: {Animals} animals, {Plants} plants",
            _animalCount, _plantCount);
    }

    #endregion
}

/// <summary>Event args for world vector changes.</summary>
public class WorldVectorChangedEventArgs : EventArgs
{
    public WorldVectorChangedEventArgs(WorldVector? oldVector, WorldVector? newVector)
    { OldVector = oldVector; NewVector = newVector; }
    public WorldVector? OldVector { get; }
    public WorldVector? NewVector { get; }
}

/// <summary>Event args for engine state changes.</summary>
public class EngineStateChangedEventArgs : EventArgs
{
    public EngineStateChangedEventArgs(string message)
    { Message = message; }
    public string Message { get; }
}
