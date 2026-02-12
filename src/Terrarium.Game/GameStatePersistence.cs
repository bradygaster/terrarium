// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using OrganismBase;

namespace Terrarium.Game;

/// <summary>
/// Default implementation of game state persistence using System.Text.Json.
/// Handles serialization/deserialization of WorldState including organisms, energy levels, tick count.
/// </summary>
public sealed class GameStatePersistence : IGameStatePersistence
{
    private readonly ILogger<GameStatePersistence> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public GameStatePersistence(ILogger<GameStatePersistence> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            Converters = { new JsonStringEnumConverter() }
        };
    }

    /// <inheritdoc/>
    public Task<string> SerializeWorldStateAsync(WorldState state, CancellationToken cancellationToken = default)
    {
        var saveData = new WorldStateSaveData
        {
            TickNumber = state.TickNumber,
            StateGuid = state.StateGuid,
            Organisms = state.Organisms.Select(org => new OrganismSaveData
            {
                ID = org.ID,
                SpeciesName = ((Species)org.Species).Name,
                AssemblyFullName = ((Species)org.Species).AssemblyFullName,
                IsAnimal = org is AnimalState,
                PositionX = org.Position.X,
                PositionY = org.Position.Y,
                GridX = org.GridX,
                GridY = org.GridY,
                EnergyState = (int)org.EnergyState,
                IsAlive = org.IsAlive,
                Age = org.TickAge,
                Generation = org.Generation,
                CellRadius = org.CellRadius,
                Radius = org.Radius
            }).ToList()
        };

        var json = JsonSerializer.Serialize(saveData, _jsonOptions);
        _logger.LogInformation("Serialized world state: {Tick} ticks, {Count} organisms, {Size} bytes",
            state.TickNumber, state.Organisms.Count, json.Length);
        
        return Task.FromResult(json);
    }

    /// <inheritdoc/>
    public Task<WorldState> DeserializeWorldStateAsync(string json, CancellationToken cancellationToken = default)
    {
        var saveData = JsonSerializer.Deserialize<WorldStateSaveData>(json, _jsonOptions);
        if (saveData is null)
        {
            throw new InvalidOperationException("Failed to deserialize world state: null result");
        }

        // Calculate grid dimensions from saved organisms
        var maxGridX = saveData.Organisms.Max(o => o.GridX) + 10; // Add padding
        var maxGridY = saveData.Organisms.Max(o => o.GridY) + 10;
        
        var worldState = new WorldState(maxGridX, maxGridY)
        {
            TickNumber = saveData.TickNumber,
            StateGuid = saveData.StateGuid
        };

        _logger.LogInformation("Deserializing world state: {Tick} ticks, {Count} organisms to restore",
            saveData.TickNumber, saveData.Organisms.Count);

        // Note: Organism restoration requires loading assemblies from PAC
        // This is handled by the caller (GameEngine.LoadGameStateAsync)
        // which has access to the PrivateAssemblyCache
        
        // For now, we return the empty world state structure
        // The caller must reconstruct organisms from the save data
        
        return Task.FromResult(worldState);
    }

    /// <inheritdoc/>
    public async Task<bool> SaveToServerAsync(string json, string saveName, CancellationToken cancellationToken = default)
    {
        // TODO: Implement server-side save endpoint integration
        // This would POST to /api/saves/{saveName} with the JSON payload
        _logger.LogWarning("SaveToServerAsync not yet implemented - save name: {SaveName}", saveName);
        await Task.CompletedTask;
        return false;
    }

    /// <inheritdoc/>
    public async Task<bool> SaveAsBrowserDownloadAsync(string json, string fileName, CancellationToken cancellationToken = default)
    {
        // TODO: Implement browser download trigger
        // In a Blazor context, this would use JSInterop to trigger a file download
        _logger.LogWarning("SaveAsBrowserDownloadAsync not yet implemented - file name: {FileName}", fileName);
        await Task.CompletedTask;
        return false;
    }

    /// <inheritdoc/>
    public async Task<string?> LoadFromServerAsync(string saveName, CancellationToken cancellationToken = default)
    {
        // TODO: Implement server-side load endpoint integration
        // This would GET from /api/saves/{saveName}
        _logger.LogWarning("LoadFromServerAsync not yet implemented - save name: {SaveName}", saveName);
        await Task.CompletedTask;
        return null;
    }
}

/// <summary>
/// Serializable representation of WorldState for save/load.
/// </summary>
public sealed class WorldStateSaveData
{
    public int TickNumber { get; set; }
    public Guid StateGuid { get; set; }
    public List<OrganismSaveData> Organisms { get; set; } = new();
}

/// <summary>
/// Serializable representation of an organism for save/load.
/// Includes only the data needed to reconstruct the organism from PAC.
/// </summary>
public sealed class OrganismSaveData
{
    public string ID { get; set; } = string.Empty;
    public string SpeciesName { get; set; } = string.Empty;
    public string AssemblyFullName { get; set; } = string.Empty;
    public bool IsAnimal { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int GridX { get; set; }
    public int GridY { get; set; }
    public int EnergyState { get; set; }
    public bool IsAlive { get; set; }
    public int Age { get; set; }
    public int Generation { get; set; }
    public int CellRadius { get; set; }
    public int Radius { get; set; }
}
