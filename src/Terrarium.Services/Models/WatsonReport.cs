namespace Terrarium.Services.Models;

public sealed class WatsonReport
{
    public required string LogType { get; init; }
    public string? OSVersion { get; init; }
    public string? GameVersion { get; init; }
    public string? CLRVersion { get; init; }
    public required string ErrorLog { get; init; }
    public string? UserEmail { get; init; }
    public string? UserComment { get; init; }
}
