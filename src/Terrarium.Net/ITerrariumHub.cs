namespace Terrarium.Net;

/// <summary>
/// Methods clients can invoke on the hub (client-to-server).
/// </summary>
public interface ITerrariumHub
{
    /// <summary>
    /// Client requests to join a specific ecosystem.
    /// Adds connection to SignalR group and registers PeerGrain.
    /// </summary>
    Task JoinEcosystem(string ecosystemId);

    /// <summary>
    /// Client requests to leave the current ecosystem.
    /// Removes from SignalR group and revokes peer lease.
    /// </summary>
    Task LeaveEcosystem(string ecosystemId);

    /// <summary>
    /// Client initiates a creature teleport to a target peer.
    /// Hub validates species, removes organism from source ecosystem,
    /// and routes to target (or random peer if TargetPeerId is null).
    /// </summary>
    Task TeleportCreature(CreatureTeleport teleport);

    /// <summary>
    /// Client announces its presence and version info.
    /// Broadcast to other peers in the ecosystem.
    /// </summary>
    Task AnnouncePeer(PeerAnnounce announce);

    /// <summary>
    /// Client requests the current world state snapshot.
    /// Returns data from EcosystemGrain — caller only.
    /// </summary>
    Task<WorldStateUpdate> RequestWorldState(string ecosystemId);

    /// <summary>
    /// Client sends a heartbeat to renew its peer lease.
    /// Should be called every 30 seconds. No broadcast.
    /// </summary>
    Task Heartbeat(string ecosystemId);

    /// <summary>
    /// Client requests the list of active peers in an ecosystem.
    /// Returns PeerListResponse to caller only.
    /// </summary>
    Task<PeerListResponse> RequestPeerList(string ecosystemId);

    /// <summary>
    /// Client reports population statistics for its local ecosystem slice.
    /// Recorded by PopulationGrain and broadcast to the ecosystem group.
    /// </summary>
    Task ReportPopulation(PopulationReport report);
}
