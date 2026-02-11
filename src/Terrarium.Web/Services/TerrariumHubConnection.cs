using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Terrarium.Net;

namespace Terrarium.Web.Services;

/// <summary>
/// Manages the SignalR connection from the Blazor client to the Terrarium server hub.
/// </summary>
public sealed class TerrariumHubConnection : IAsyncDisposable
{
    private readonly HubConnection _hub;
    private readonly ILogger<TerrariumHubConnection> _logger;

    /// <summary>Fires when a full world state snapshot arrives.</summary>
    public event Func<WorldStateUpdate, Task>? OnWorldStateReceived;

    /// <summary>Fires on each ecosystem tick with lightweight creature data.</summary>
    public event Func<EcosystemTick, Task>? OnCreatureUpdate;

    /// <summary>Fires when a peer joins or leaves the ecosystem.</summary>
    public event Func<PeerAnnounce, Task>? OnPeerDiscovered;

    public TerrariumHubConnection(IConfiguration configuration, ILogger<TerrariumHubConnection> logger)
    {
        _logger = logger;

        var serverUrl = configuration["services:server:https:0"]
                        ?? configuration["services:server:http:0"]
                        ?? "https+http://server";

        var hubUrl = $"{serverUrl}/terrarium-hub";

        _hub = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _hub.On<WorldStateUpdate>(nameof(ITerrariumClient.ReceiveWorldStateUpdate), async update =>
        {
            if (OnWorldStateReceived is not null)
                await OnWorldStateReceived.Invoke(update);
        });

        _hub.On<EcosystemTick>(nameof(ITerrariumClient.ReceiveEcosystemTick), async tick =>
        {
            if (OnCreatureUpdate is not null)
                await OnCreatureUpdate.Invoke(tick);
        });

        _hub.On<PeerAnnounce>(nameof(ITerrariumClient.ReceivePeerAnnounce), async announce =>
        {
            if (OnPeerDiscovered is not null)
                await OnPeerDiscovered.Invoke(announce);
        });
    }

    /// <summary>Establish the SignalR hub connection.</summary>
    public async Task ConnectAsync()
    {
        try
        {
            await _hub.StartAsync();
            _logger.LogInformation("Connected to Terrarium hub");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Terrarium hub");
        }
    }

    /// <summary>Cleanly shut down the hub connection.</summary>
    public async Task DisconnectAsync()
    {
        try
        {
            await _hub.StopAsync();
            _logger.LogInformation("Disconnected from Terrarium hub");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error during hub disconnect");
        }
    }

    /// <summary>Announce this client as a peer in the given ecosystem.</summary>
    public async Task RegisterAsPeer(string ecosystemId = "default")
    {
        var announce = new PeerAnnounce
        {
            PeerId = _hub.ConnectionId ?? "unknown",
            EcosystemId = ecosystemId,
            Action = PeerAction.Join
        };

        await _hub.InvokeAsync(nameof(ITerrariumHub.AnnouncePeer), announce);
        _logger.LogInformation("Registered as peer in ecosystem {EcosystemId}", ecosystemId);
    }

    /// <summary>Submit creature actions to the server (placeholder for future game loop).</summary>
    public async Task SendActions(CreatureTeleport teleport)
    {
        await _hub.InvokeAsync(nameof(ITerrariumHub.TeleportCreature), teleport);
    }

    public async ValueTask DisposeAsync()
    {
        await _hub.DisposeAsync();
    }
}
