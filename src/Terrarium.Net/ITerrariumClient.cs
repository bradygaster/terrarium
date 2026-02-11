namespace Terrarium.Net;

/// <summary>
/// Methods the server can invoke on connected clients (server-to-client).
/// </summary>
public interface ITerrariumClient
{
    /// <summary>
    /// Pushes an ecosystem tick snapshot to the client.
    /// </summary>
    Task ReceiveEcosystemTick(EcosystemTick tick);

    /// <summary>
    /// Pushes a full or partial world state update to the client.
    /// </summary>
    Task ReceiveWorldStateUpdate(WorldStateUpdate update);

    /// <summary>
    /// Delivers a teleported creature to the client.
    /// </summary>
    Task ReceiveCreatureTeleport(CreatureTeleport teleport);

    /// <summary>
    /// Announces a peer joining or leaving the ecosystem.
    /// </summary>
    Task ReceivePeerAnnounce(PeerAnnounce announce);
}
