namespace Terrarium.Services.Models;

public sealed class SpeciesInfo
{
    public required string Name { get; init; }
    public required string Author { get; init; }
    public required string AuthorEmail { get; init; }
    public required OrganismType Type { get; init; }
    public required string Version { get; init; }
    public required string AssemblyFullName { get; init; }
    public bool IsExtinct { get; init; }
    public bool IsBlacklisted { get; init; }
    public int Population { get; init; }
    public DateTime? DateAdded { get; init; }
    public DateTime? LastReintroduction { get; init; }
}
