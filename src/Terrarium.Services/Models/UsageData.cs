namespace Terrarium.Services.Models;

public sealed class UsageData
{
    public string? Alias { get; init; }
    public string? Domain { get; init; }
    public string? GameVersion { get; init; }
    public string? PeerChannel { get; init; }
    public int PeerCount { get; init; }
    public int AnimalCount { get; init; }
    public int MaxAnimalCount { get; init; }
    public int WorldWidth { get; init; }
    public int WorldHeight { get; init; }
    public string? MachineName { get; init; }
    public string? OSVersion { get; init; }
    public int ProcessorCount { get; init; }
    public string? ClrVersion { get; init; }
    public int WorkingSet { get; init; }
    public int MaxWorkingSet { get; init; }
    public int MinWorkingSet { get; init; }
    public int ProcessorTimeInSeconds { get; init; }
    public DateTime ProcessStartTime { get; init; }
}
