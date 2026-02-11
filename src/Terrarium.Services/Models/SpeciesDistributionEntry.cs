namespace Terrarium.Services.Models;

public sealed class SpeciesDistributionEntry
{
    public string SpeciesName { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string AuthorName { get; init; } = string.Empty;
    public int Population { get; init; }
}
