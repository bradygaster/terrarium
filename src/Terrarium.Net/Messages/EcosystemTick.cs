namespace Terrarium.Net;

/// <summary>
/// Lightweight tick notification broadcast to all clients in an ecosystem.
/// The full world state is in <see cref="WorldStateUpdate"/>; this is the clock signal.
/// </summary>
public sealed class EcosystemTick
{
    /// <summary>
    /// The ecosystem this tick belongs to.
    /// </summary>
    public required string EcosystemId { get; init; }

    /// <summary>
    /// Monotonically increasing tick number.
    /// </summary>
    public required long TickNumber { get; init; }

    /// <summary>
    /// Duration of this tick in milliseconds (for client-side interpolation).
    /// </summary>
    public int TickDurationMs { get; init; }

    /// <summary>
    /// Number of active peers in this ecosystem at this tick.
    /// </summary>
    public int PeerCount { get; init; }

    /// <summary>
    /// Total organism count at this tick.
    /// </summary>
    public int OrganismCount { get; init; }

    /// <summary>
    /// UTC timestamp when this tick was processed.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
