namespace Terrarium.Net;

/// <summary>
/// Represents a creature being teleported between peers.
/// Maps to the legacy 4-step teleport protocol (version check, assembly check,
/// assembly transfer, state transfer) collapsed into a single message.
/// </summary>
public sealed class CreatureTeleport
{
    /// <summary>
    /// Unique identifier for this teleport operation.
    /// </summary>
    public required string TeleportId { get; init; }

    /// <summary>
    /// The ecosystem the creature belongs to.
    /// </summary>
    public required string EcosystemId { get; init; }

    /// <summary>
    /// Unique organism identifier.
    /// </summary>
    public required string OrganismId { get; init; }

    /// <summary>
    /// Assembly-qualified type name of the organism.
    /// </summary>
    public required string SpeciesAssemblyName { get; init; }

    /// <summary>
    /// Connection ID of the originating peer.
    /// </summary>
    public required string SourcePeerId { get; init; }

    /// <summary>
    /// Connection ID of the target peer, if directed. Null for random placement.
    /// </summary>
    public string? TargetPeerId { get; init; }

    /// <summary>
    /// Serialized organism state (JSON). Opaque to the hub — interpreted by the client.
    /// </summary>
    public required string StatePayload { get; init; }

    /// <summary>
    /// Base64-encoded assembly bytes, included when the target peer lacks the assembly.
    /// </summary>
    public string? AssemblyPayload { get; init; }

    /// <summary>
    /// UTC timestamp of the teleport initiation.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
