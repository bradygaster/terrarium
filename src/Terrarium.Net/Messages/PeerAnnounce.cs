namespace Terrarium.Net;

/// <summary>
/// Peer announcement message for join/leave/heartbeat.
/// Replaces the legacy PeerDiscoveryService + HTTP version check.
/// </summary>
public sealed class PeerAnnounce
{
    /// <summary>
    /// SignalR connection ID of the peer.
    /// </summary>
    public required string PeerId { get; init; }

    /// <summary>
    /// The ecosystem the peer is joining or leaving.
    /// </summary>
    public required string EcosystemId { get; init; }

    /// <summary>
    /// Type of announcement.
    /// </summary>
    public required PeerAction Action { get; init; }

    /// <summary>
    /// Client version string for compatibility checks.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Client channel (e.g., "stable", "preview") for version gating.
    /// </summary>
    public string? Channel { get; init; }

    /// <summary>
    /// UTC timestamp of the announcement.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// The type of peer lifecycle event.
/// </summary>
public enum PeerAction
{
    Join,
    Leave,
    Heartbeat
}
