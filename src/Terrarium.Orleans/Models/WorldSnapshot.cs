namespace Terrarium.Orleans.Models;

/// <summary>
/// Immutable snapshot of the ecosystem world state at a point in time.
/// </summary>
[GenerateSerializer]
public sealed class WorldSnapshot
{
    [Id(0)] public required string EcosystemId { get; init; }
    [Id(1)] public long TickNumber { get; init; }
    [Id(2)] public int WorldWidth { get; init; }
    [Id(3)] public int WorldHeight { get; init; }
    [Id(4)] public int OrganismCount { get; init; }
    [Id(5)] public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
