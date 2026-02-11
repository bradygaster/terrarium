namespace Terrarium.Services.Models;

public sealed class DailyStats
{
    public int ActivePeers { get; init; }
    public int SpeciesCount { get; init; }
    public int TotalPopulation { get; init; }
    public DateTime? LastRollup { get; init; }
}
