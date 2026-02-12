namespace Terrarium.Orleans.Models;

/// <summary>
/// Metadata for a registered species in the global catalog.
/// </summary>
[GenerateSerializer]
public sealed class SpeciesInfo
{
    [Id(0)] public required string Name { get; init; }
    [Id(1)] public required string AssemblyHash { get; init; }
    [Id(2)] public DateTimeOffset RegisteredAt { get; init; }
    [Id(3)] public bool IsBlacklisted { get; init; }
    [Id(4)] public string? BlacklistReason { get; init; }
}
