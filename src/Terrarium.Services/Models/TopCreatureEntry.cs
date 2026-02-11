namespace Terrarium.Services.Models;

public sealed class TopCreatureEntry
{
    public string Name { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public int Population { get; init; }
    public int KilledCount { get; init; }
}
