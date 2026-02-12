// Copyright (c) Microsoft Corporation.  All rights reserved.

using Microsoft.Extensions.Logging;
using OrganismBase;

namespace Terrarium.Game.Rendering;

/// <summary>
/// Converts engine WorldState into WorldRenderData and dispatches to IEngineRenderer.
/// Called by GameEngine after each completed tick.
/// </summary>
public sealed class GameRenderBridge
{
    private readonly IEngineRenderer _renderer;
    private readonly ILogger<GameRenderBridge> _logger;

    public GameRenderBridge(IEngineRenderer renderer, ILogger<GameRenderBridge> logger)
    {
        _renderer = renderer;
        _logger = logger;
    }

    /// <summary>
    /// Extracts render data from the current world vector and sends to the renderer.
    /// </summary>
    public async Task RenderTickAsync(GameEngine engine, CancellationToken cancellationToken = default)
    {
        if (!_renderer.IsReady) return;

        var vector = engine.CurrentVector;
        if (vector is null) return;

        var state = vector.State;
        var organisms = new List<OrganismRenderData>(state.Organisms.Count);

        foreach (var org in state.ZOrderedOrganisms)
        {
            var species = (Species)org.Species;
            var skinFamily = species switch
            {
                AnimalSpecies a => a.SkinFamily.ToString().ToLowerInvariant(),
                PlantSpecies p => p.SkinFamily.ToString().ToLowerInvariant(),
                _ => species.Name
            };

            organisms.Add(new OrganismRenderData
            {
                Id = org.ID,
                SpeciesName = species.Name,
                SkinFamily = skinFamily,
                X = org.Position.X,
                Y = org.Position.Y,
                Radius = org.Radius,
                Energy = (int)org.StoredEnergy,
                MaxEnergy = (int)OrganismState.UpperBoundaryForEnergyState(org.Species, EnergyState.Normal, org.Radius),
                IsAlive = org.IsAlive,
                IsPlant = org is PlantState,
                CurrentAction = org.PreviousDisplayAction
            });
        }

        var teleportZones = new List<TeleportZoneRenderData>();
        if (state.Teleporter is not null)
        {
            var zones = state.Teleporter.GetTeleportZones();
            for (var i = 0; i < zones.Length; i++)
            {
                var zone = zones[i];
                var rect = zone.Rectangle;
                teleportZones.Add(new TeleportZoneRenderData
                {
                    Id = i,
                    X = rect.X,
                    Y = rect.Y,
                    Width = rect.Width,
                    Height = rect.Height
                });
            }
        }

        var renderData = new WorldRenderData
        {
            TickNumber = state.TickNumber,
            WorldWidth = engine.WorldWidth,
            WorldHeight = engine.WorldHeight,
            Organisms = organisms,
            TeleportZones = teleportZones,
            AnimalCount = engine.AnimalCount,
            PlantCount = engine.PlantCount,
            StatusText = $"Tick {state.TickNumber} | Animals: {engine.AnimalCount} | Plants: {engine.PlantCount}"
        };

        try
        {
            await _renderer.RenderWorldAsync(renderData, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Render failed at tick {Tick}", state.TickNumber);
        }
    }
}
