namespace Terrarium.Net;

/// <summary>
/// Methods the server can invoke on connected clients (server-to-client).
/// </summary>
public interface ITerrariumClient
{
    /// <summary>
    /// Pushes an ecosystem tick snapshot to the client.
    /// Broadcast to all clients in the ecosystem group.
    /// </summary>
    Task ReceiveEcosystemTick(EcosystemTick tick);

    /// <summary>
    /// Pushes a full or partial world state update to the client.
    /// Sent to caller on request, or broadcast after each tick.
    /// </summary>
    Task ReceiveWorldStateUpdate(WorldStateUpdate update);

    /// <summary>
    /// Delivers a teleported creature to the client.
    /// Targeted to a specific connection.
    /// </summary>
    Task ReceiveCreatureTeleport(CreatureTeleport teleport);

    /// <summary>
    /// Announces a peer joining or leaving the ecosystem.
    /// Broadcast to all other clients in the ecosystem group.
    /// </summary>
    Task ReceivePeerAnnounce(PeerAnnounce announce);

    /// <summary>
    /// Returns the list of active peers in an ecosystem.
    /// Sent to caller only in response to RequestPeerList.
    /// </summary>
    Task ReceivePeerList(PeerListResponse peerList);

    /// <summary>
    /// Pushes population statistics to the client.
    /// Broadcast to all clients in the ecosystem group.
    /// </summary>
    Task ReceivePopulationReport(PopulationReport report);

    /// <summary>
    /// Delivers an error to the client. Sent to caller only.
    /// Replaces throwing exceptions from hub methods.
    /// </summary>
    Task ReceiveError(HubError error);
}
