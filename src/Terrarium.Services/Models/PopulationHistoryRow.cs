namespace Terrarium.Services.Models;

public sealed class PopulationHistoryRow
{
    public Guid Guid { get; init; }
    public int TickNumber { get; init; }
    public required string SpeciesName { get; init; }
    public DateTime ClientTime { get; init; }
    public int CorrectTime { get; init; }
    public int Population { get; init; }
    public int BirthCount { get; init; }
    public int TeleportedToCount { get; init; }
    public int StarvedCount { get; init; }
    public int KilledCount { get; init; }
    public int TeleportedFromCount { get; init; }
    public int ErrorCount { get; init; }
    public int TimeoutCount { get; init; }
    public int SickCount { get; init; }
    public int OldAgeCount { get; init; }
    public int SecurityViolationCount { get; init; }
}
