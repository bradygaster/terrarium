// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using OrganismBase;
using Terrarium.Configuration;

namespace Terrarium.Game;

/// <summary>
/// The core Terrarium game engine. Controls creatures, updates world data,
/// and manages all game events. The 10-phase ProcessTurn() method is the heart
/// of the game loop.
/// </summary>
public class GameEngine
{
    private readonly ILogger<GameEngine> _logger;
    private readonly Random _random = new(DateTime.Now.Millisecond);
    private readonly ConcurrentQueue<object> _newOrganismQueue = new();
    private readonly Queue<KilledOrganism> _removeOrganismQueue = new();

    private int _maxAnimals = 50;
    private int _maxPlants = 50;
    private int _worldHeight = 2048;
    private int _worldWidth = 2048;
    private int _animalCount;
    private int _plantCount;
    private int _turnPhase;

    private WorldVector? _currentVector;
    private WorldState? _newWorldState;
    private string[]? _organismIDList;
    private PopulationData? _populationData;
    private bool _ecosystemMode;

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

    /// <summary>Population data tracker.</summary>
    public PopulationData? PopulationData => _populationData;

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

        _turnPhase++;
        if (_turnPhase == 10)
        {
            _turnPhase = 0;
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
        foreach (var organismID in _organismIDList!)
        {
            var organismState = _newWorldState!.GetOrganismState(organismID);
            if (organismState == null) continue;
            if (organismState is AnimalState && !organismState.IsAlive) continue;
            if (_newWorldState.Teleporter != null && _newWorldState.Teleporter.IsInTeleporter(organismState))
            {
                // TODO: Sprint 7 — teleportation via network engine / Orleans
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

    private void OnEngineStateChanged(EngineStateChangedEventArgs e)
    {
        _logger.LogDebug("Engine state changed: {Message}", e.Message);
        EngineStateChanged?.Invoke(this, e);
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
