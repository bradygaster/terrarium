using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace Terrarium.Net;

/// <summary>
/// SignalR hub for real-time Terrarium ecosystem communication.
/// Thin relay layer — all stateful logic will be delegated to Orleans grains in Sprint 7.
/// </summary>
public class TerrariumHub : Hub<ITerrariumClient>, ITerrariumHub
{
    private readonly ILogger<TerrariumHub> _logger;

    public TerrariumHub(ILogger<TerrariumHub> logger)
    {
        _logger = logger;
    }

    public async Task JoinEcosystem(string ecosystemId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, ecosystemId);
        _logger.LogInformation("Peer {ConnectionId} joined ecosystem {EcosystemId}", Context.ConnectionId, ecosystemId);

        var announce = new PeerAnnounce
        {
            PeerId = Context.ConnectionId,
            EcosystemId = ecosystemId,
            Action = PeerAction.Join
        };

        await Clients.OthersInGroup(ecosystemId).ReceivePeerAnnounce(announce);
    }

    public async Task LeaveEcosystem(string ecosystemId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, ecosystemId);
        _logger.LogInformation("Peer {ConnectionId} left ecosystem {EcosystemId}", Context.ConnectionId, ecosystemId);

        var announce = new PeerAnnounce
        {
            PeerId = Context.ConnectionId,
            EcosystemId = ecosystemId,
            Action = PeerAction.Leave
        };

        await Clients.OthersInGroup(ecosystemId).ReceivePeerAnnounce(announce);
    }

    public async Task TeleportCreature(CreatureTeleport teleport)
    {
        _logger.LogInformation(
            "Teleport {TeleportId}: organism {OrganismId} from {Source} in ecosystem {EcosystemId}",
            teleport.TeleportId, teleport.OrganismId, teleport.SourcePeerId, teleport.EcosystemId);

        // TODO: Sprint 7 — delegate to PeerGrain / EcosystemGrain for validation and routing
        if (teleport.TargetPeerId is not null)
        {
            await Clients.Client(teleport.TargetPeerId).ReceiveCreatureTeleport(teleport);
        }
        else
        {
            // Broadcast to ecosystem group; Orleans will handle targeted routing later
            await Clients.OthersInGroup(teleport.EcosystemId).ReceiveCreatureTeleport(teleport);
        }
    }

    public async Task AnnouncePeer(PeerAnnounce announce)
    {
        _logger.LogInformation(
            "Peer announce: {PeerId} {Action} in {EcosystemId}",
            announce.PeerId, announce.Action, announce.EcosystemId);

        // TODO: Sprint 7 — register with PeerGrain for lease management
        await Clients.OthersInGroup(announce.EcosystemId).ReceivePeerAnnounce(announce);
    }

    public Task<WorldStateUpdate> RequestWorldState(string ecosystemId)
    {
        _logger.LogInformation("World state requested by {ConnectionId} for {EcosystemId}", Context.ConnectionId, ecosystemId);

        // TODO: Sprint 7 — fetch from EcosystemGrain
        return Task.FromResult(new WorldStateUpdate
        {
            EcosystemId = ecosystemId,
            TickNumber = 0,
            WorldWidth = 0,
            WorldHeight = 0,
            OrganismCount = 0
        });
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Peer {ConnectionId} disconnected", Context.ConnectionId);
        // TODO: Sprint 7 — notify PeerGrain to revoke lease
        await base.OnDisconnectedAsync(exception);
    }
}
