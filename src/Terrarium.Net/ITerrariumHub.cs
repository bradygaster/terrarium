namespace Terrarium.Net;

/// <summary>
/// Methods clients can invoke on the hub (client-to-server).
/// </summary>
public interface ITerrariumHub
{
    /// <summary>
    /// Client requests to join a specific ecosystem.
    /// </summary>
    Task JoinEcosystem(string ecosystemId);

    /// <summary>
    /// Client requests to leave the current ecosystem.
    /// </summary>
    Task LeaveEcosystem(string ecosystemId);

    /// <summary>
    /// Client initiates a creature teleport to a target peer.
    /// </summary>
    Task TeleportCreature(CreatureTeleport teleport);

    /// <summary>
    /// Client announces its presence and version info.
    /// </summary>
    Task AnnouncePeer(PeerAnnounce announce);

    /// <summary>
    /// Client requests the current world state snapshot.
    /// </summary>
    Task<WorldStateUpdate> RequestWorldState(string ecosystemId);
}
