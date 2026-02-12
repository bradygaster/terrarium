// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Drawing;
using Terrarium.Game.Networking;
using Terrarium.Game.Rendering;
using Terrarium.Game.Services;

namespace Terrarium.Game;

/// <summary>
/// Abstraction over the Terrarium game engine for DI and testability.
/// </summary>
public interface IGameEngine
{
    int WorldHeight { get; }
    int WorldWidth { get; }
    int GridWidth { get; }
    int GridHeight { get; }
    int MaxAnimals { get; }
    int MaxPlants { get; }
    int AnimalCount { get; }
    int PlantCount { get; }
    int TurnPhase { get; }
    bool EcosystemMode { get; }
    Game.EcosystemMode Mode { get; set; }
    PopulationData? PopulationData { get; }
    WorldVector? CurrentVector { get; set; }

    /// <summary>Render bridge for tick-based rendering dispatch.</summary>
    GameRenderBridge? RenderBridge { get; set; }

    /// <summary>Network bridge for SignalR P2P communication.</summary>
    GameNetworkBridge? NetworkBridge { get; set; }

    /// <summary>Service bridge for server HTTP communication.</summary>
    GameServiceBridge? ServiceBridge { get; set; }

    /// <summary>Game state persistence handler for save/load operations.</summary>
    IGameStatePersistence? StatePersistence { get; set; }

    event EventHandler<WorldVectorChangedEventArgs>? WorldVectorChanged;
    event EventHandler<EngineStateChangedEventArgs>? EngineStateChanged;

    bool ProcessTurn();
    void RemoveOrganismQueued(KilledOrganism killedOrganism);
    void AddNewOrganism(Species species, Point preferredLocation);

    bool IntroduceCreatureFromPac(
        string assemblyFullName,
        Hosting.PrivateAssemblyCache pac,
        Hosting.AssemblyValidator? validator = null,
        Point? preferredLocation = null);

    Task<bool> IntroduceCreatureFromServerAsync(
        string speciesName,
        string version,
        Hosting.PrivateAssemblyCache pac,
        Hosting.AssemblyValidator? validator = null,
        Point? preferredLocation = null,
        CancellationToken cancellationToken = default);

    void StopGame(bool serializeState);

    Task<string> SaveGameStateAsync(CancellationToken cancellationToken = default);
    Task LoadGameStateAsync(string json, Hosting.PrivateAssemblyCache pac, CancellationToken cancellationToken = default);
}
