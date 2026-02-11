namespace Terrarium.Services.Models;

public sealed class SpeciesPopulationData
{
    public DateTime SampleDateTime { get; init; }
    public string SpeciesName { get; init; } = string.Empty;
    public int Population { get; init; }
    public int BirthCount { get; init; }
    public int StarvedCount { get; init; }
    public int KilledCount { get; init; }
    public int ErrorCount { get; init; }
    public int TimeoutCount { get; init; }
    public int SickCount { get; init; }
    public int OldAgeCount { get; init; }
    public int SecurityViolationCount { get; init; }
}
