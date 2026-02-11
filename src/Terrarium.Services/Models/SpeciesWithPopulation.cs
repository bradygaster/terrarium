namespace Terrarium.Services.Models;

public sealed class SpeciesWithPopulation
{
    public string SpeciesName { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public string AuthorEmail { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public int Population { get; init; }
    public string Type { get; init; } = string.Empty;
}
