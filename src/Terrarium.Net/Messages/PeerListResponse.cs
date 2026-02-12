namespace Terrarium.Net;

/// <summary>
/// Response to a peer list request — active peers in an ecosystem.
/// </summary>
public sealed class PeerListResponse
{
    /// <summary>
    /// The ecosystem these peers belong to.
    /// </summary>
    public required string EcosystemId { get; init; }

    /// <summary>
    /// Active peers in the ecosystem.
    /// </summary>
    public required IReadOnlyList<PeerInfo> Peers { get; init; }

    /// <summary>
    /// UTC timestamp when this list was generated.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Summary of a connected peer, returned in peer list responses.
/// </summary>
public sealed class PeerInfo
{
    /// <summary>
    /// SignalR connection ID of the peer.
    /// </summary>
    public required string PeerId { get; init; }

    /// <summary>
    /// Client version string.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// When the peer joined the ecosystem.
    /// </summary>
    public DateTimeOffset ConnectedAt { get; init; }
}
