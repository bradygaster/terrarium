namespace Terrarium.Orleans.Models;

/// <summary>
/// Describes a connected peer in the ecosystem.
/// </summary>
[GenerateSerializer]
public sealed class PeerInfo
{
    [Id(0)] public required string PeerId { get; init; }
    [Id(1)] public string? Endpoint { get; init; }
    [Id(2)] public string? Version { get; init; }
    [Id(3)] public DateTimeOffset LastHeartbeat { get; init; }
    [Id(4)] public DateTimeOffset ConnectedAt { get; init; }
}
