namespace Terrarium.Services.Models;

public sealed class BugReport
{
    public required string Title { get; init; }
    public required string Description { get; init; }
    public string? Path { get; init; }
    public string? Alias { get; init; }
    public string? Version { get; init; }
}
