// Copyright (c) Microsoft Corporation.  All rights reserved.

using OrganismBase;

namespace Terrarium.Game.Rendering;

/// <summary>
/// Engine-side abstraction for rendering. Lives in Terrarium.Game so the engine
/// has no dependency on Terrarium.Web or any UI framework. Implementations
/// (e.g., CanvasGameRenderer via a bridge) register at DI time.
/// </summary>
public interface IEngineRenderer
{
    /// <summary>Whether the renderer is ready to accept frames.</summary>
    bool IsReady { get; }

    /// <summary>
    /// Renders the current world state. Called once per completed tick (every 10 phases).
    /// </summary>
    Task RenderWorldAsync(WorldRenderData data, CancellationToken cancellationToken = default);
}

/// <summary>
/// Complete render payload produced by the engine each tick.
/// </summary>
public sealed class WorldRenderData
{
    public required int TickNumber { get; init; }
    public required int WorldWidth { get; init; }
    public required int WorldHeight { get; init; }
    public required IReadOnlyList<OrganismRenderData> Organisms { get; init; }
    public required IReadOnlyList<TeleportZoneRenderData> TeleportZones { get; init; }
    public string? StatusText { get; init; }
    public int AnimalCount { get; init; }
    public int PlantCount { get; init; }
}

/// <summary>
/// Per-organism data for rendering.
/// </summary>
public sealed class OrganismRenderData
{
    public required string Id { get; init; }
    public required string SpeciesName { get; init; }
    public required string SkinFamily { get; init; }
    public required float X { get; init; }
    public required float Y { get; init; }
    public required int Radius { get; init; }
    public required int Energy { get; init; }
    public required int MaxEnergy { get; init; }
    public required bool IsAlive { get; init; }
    public required bool IsPlant { get; init; }
    public DisplayAction CurrentAction { get; init; }
}

/// <summary>
/// Teleport zone position data for rendering.
/// </summary>
public sealed class TeleportZoneRenderData
{
    public required int Id { get; init; }
    public required float X { get; init; }
    public required float Y { get; init; }
    public required float Width { get; init; }
    public required float Height { get; init; }
}
