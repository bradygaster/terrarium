namespace Terrarium.Services.Models;

public sealed class PopulationData
{
    public required Guid PeerGuid { get; init; }
    public required int CurrentTick { get; init; }
    public required IReadOnlyList<PopulationEntry> Entries { get; init; }
}

public sealed class PopulationEntry
{
    public required string SpeciesName { get; init; }
    public required int Population { get; init; }
    public required string Version { get; init; }
}
