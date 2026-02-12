namespace Terrarium.Orleans.Models;

/// <summary>
/// A point-in-time population measurement for an ecosystem.
/// </summary>
[GenerateSerializer]
public sealed class PopulationSnapshot
{
    [Id(0)] public required string EcosystemId { get; init; }
    [Id(1)] public long TickNumber { get; init; }
    [Id(2)] public int TotalOrganisms { get; init; }
    [Id(3)] public int SpeciesCount { get; init; }
    [Id(4)] public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
