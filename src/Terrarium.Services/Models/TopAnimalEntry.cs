namespace Terrarium.Services.Models;

public sealed class TopAnimalEntry
{
    public string Name { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public int Population { get; init; }
}
