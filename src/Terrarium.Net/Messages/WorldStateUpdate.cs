namespace Terrarium.Net;

/// <summary>
/// A snapshot of the world state pushed to clients after each tick.
/// </summary>
public sealed class WorldStateUpdate
{
    /// <summary>
    /// The ecosystem this update belongs to.
    /// </summary>
    public required string EcosystemId { get; init; }

    /// <summary>
    /// Tick number for ordering and dedup.
    /// </summary>
    public required long TickNumber { get; init; }

    /// <summary>
    /// Width of the world grid in cells.
    /// </summary>
    public int WorldWidth { get; init; }

    /// <summary>
    /// Height of the world grid in cells.
    /// </summary>
    public int WorldHeight { get; init; }

    /// <summary>
    /// Total organism count at this tick.
    /// </summary>
    public int OrganismCount { get; init; }

    /// <summary>
    /// Per-creature position/type data for client-side rendering.
    /// </summary>
    public List<CreatureStateData> Creatures { get; init; } = [];

    /// <summary>
    /// UTC timestamp when this snapshot was taken.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Per-creature data included in <see cref="WorldStateUpdate"/> for rendering.
/// Matches the shape expected by the JS canvas renderer (CreatureRenderData).
/// </summary>
public sealed class CreatureStateData
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Species { get; set; } = "";
    public string SkinFamily { get; set; } = "";
    public float X { get; set; }
    public float Y { get; set; }
    public int Energy { get; set; }
}
