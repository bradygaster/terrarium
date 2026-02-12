using Microsoft.AspNetCore.SignalR.Client;
using Terrarium.Net;

namespace Terrarium.Web.Services;

/// <summary>
/// SignalR client service that connects the Blazor web app to TerrariumHub.
/// Manages connection lifecycle, auto-reconnect with exponential backoff,
/// and exposes hub methods and client callbacks as C# events.
/// </summary>
public sealed class TerrariumHubClient : IAsyncDisposable
{
    private readonly HubConnection _hubConnection;
    private readonly ILogger<TerrariumHubClient> _logger;

    /// <summary>
    /// Current connection state.
    /// </summary>
    public HubConnectionState State => _hubConnection.State;

    /// <summary>
    /// The connection ID assigned by the server, or null if not connected.
    /// </summary>
    public string? ConnectionId => _hubConnection.ConnectionId;

    // --- Client callback events (server-to-client) ---

    public event Func<EcosystemTick, Task>? OnEcosystemTick;
    public event Func<WorldStateUpdate, Task>? OnWorldStateUpdate;
    public event Func<CreatureTeleport, Task>? OnCreatureTeleport;
    public event Func<PeerAnnounce, Task>? OnPeerAnnounce;
    public event Func<PeerListResponse, Task>? OnPeerList;
    public event Func<PopulationReport, Task>? OnPopulationReport;
    public event Func<HubError, Task>? OnError;

    // --- Connection state events ---

    public event Func<string?, Task>? OnReconnecting;
    public event Func<string?, Task>? OnReconnected;
    public event Func<Exception?, Task>? OnClosed;
    public event Action<HubConnectionState>? OnStateChanged;

    public TerrariumHubClient(ILogger<TerrariumHubClient> logger, IConfiguration configuration)
    {
        _logger = logger;

        var serverUrl = configuration["Services:server:https:0"]
            ?? configuration["Services:server:http:0"]
            ?? "https+http://server";

        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{serverUrl}/terrarium")
            .WithAutomaticReconnect(new ExponentialBackoffRetryPolicy())
            .Build();

        RegisterCallbacks();
        RegisterLifecycleHandlers();
    }

    private void RegisterCallbacks()
    {
        _hubConnection.On<EcosystemTick>(nameof(ITerrariumClient.ReceiveEcosystemTick), async tick =>
        {
            _logger.LogDebug("Received ecosystem tick {TickNumber}", tick.TickNumber);
            if (OnEcosystemTick is not null) await OnEcosystemTick(tick);
        });

        _hubConnection.On<WorldStateUpdate>(nameof(ITerrariumClient.ReceiveWorldStateUpdate), async update =>
        {
            _logger.LogDebug("Received world state update for {EcosystemId}", update.EcosystemId);
            if (OnWorldStateUpdate is not null) await OnWorldStateUpdate(update);
        });

        _hubConnection.On<CreatureTeleport>(nameof(ITerrariumClient.ReceiveCreatureTeleport), async teleport =>
        {
            _logger.LogInformation("Received creature teleport {TeleportId}", teleport.TeleportId);
            if (OnCreatureTeleport is not null) await OnCreatureTeleport(teleport);
        });

        _hubConnection.On<PeerAnnounce>(nameof(ITerrariumClient.ReceivePeerAnnounce), async announce =>
        {
            _logger.LogInformation("Peer {PeerId} {Action} in {EcosystemId}", announce.PeerId, announce.Action, announce.EcosystemId);
            if (OnPeerAnnounce is not null) await OnPeerAnnounce(announce);
        });

        _hubConnection.On<PeerListResponse>(nameof(ITerrariumClient.ReceivePeerList), async peerList =>
        {
            _logger.LogDebug("Received peer list with {Count} peers", peerList.Peers.Count);
            if (OnPeerList is not null) await OnPeerList(peerList);
        });

        _hubConnection.On<PopulationReport>(nameof(ITerrariumClient.ReceivePopulationReport), async report =>
        {
            _logger.LogDebug("Received population report: {Total} organisms", report.TotalOrganisms);
            if (OnPopulationReport is not null) await OnPopulationReport(report);
        });

        _hubConnection.On<HubError>(nameof(ITerrariumClient.ReceiveError), async error =>
        {
            _logger.LogWarning("Hub error [{Code}]: {Message} (transient: {IsTransient})", error.Code, error.Message, error.IsTransient);
            if (OnError is not null) await OnError(error);
        });
    }

    private void RegisterLifecycleHandlers()
    {
        _hubConnection.Reconnecting += error =>
        {
            _logger.LogWarning(error, "SignalR reconnecting...");
            OnStateChanged?.Invoke(HubConnectionState.Reconnecting);
            return OnReconnecting?.Invoke(error?.Message) ?? Task.CompletedTask;
        };

        _hubConnection.Reconnected += connectionId =>
        {
            _logger.LogInformation("SignalR reconnected with connection ID {ConnectionId}", connectionId);
            OnStateChanged?.Invoke(HubConnectionState.Connected);
            return OnReconnected?.Invoke(connectionId) ?? Task.CompletedTask;
        };

        _hubConnection.Closed += error =>
        {
            _logger.LogWarning(error, "SignalR connection closed");
            OnStateChanged?.Invoke(HubConnectionState.Disconnected);
            return OnClosed?.Invoke(error) ?? Task.CompletedTask;
        };
    }

    // --- Connection management ---

    /// <summary>
    /// Starts the SignalR connection.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_hubConnection.State != HubConnectionState.Disconnected)
        {
            _logger.LogDebug("Connection already in state {State}, skipping start", _hubConnection.State);
            return;
        }

        _logger.LogInformation("Starting SignalR connection...");
        OnStateChanged?.Invoke(HubConnectionState.Connecting);

        try
        {
            await _hubConnection.StartAsync(cancellationToken);
            _logger.LogInformation("SignalR connected with ID {ConnectionId}", _hubConnection.ConnectionId);
            OnStateChanged?.Invoke(HubConnectionState.Connected);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start SignalR connection");
            OnStateChanged?.Invoke(HubConnectionState.Disconnected);
            throw;
        }
    }

    /// <summary>
    /// Stops the SignalR connection gracefully.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_hubConnection.State == HubConnectionState.Disconnected)
            return;

        _logger.LogInformation("Stopping SignalR connection...");
        await _hubConnection.StopAsync(cancellationToken);
    }

    // --- Hub method invocations (client-to-server) ---

    /// <summary>
    /// Joins an ecosystem, subscribing to its events.
    /// </summary>
    public async Task JoinEcosystemAsync(string ecosystemId)
    {
        _logger.LogInformation("Joining ecosystem {EcosystemId}", ecosystemId);
        await _hubConnection.InvokeAsync(nameof(ITerrariumHub.JoinEcosystem), ecosystemId);
    }

    /// <summary>
    /// Leaves an ecosystem, unsubscribing from its events.
    /// </summary>
    public async Task LeaveEcosystemAsync(string ecosystemId)
    {
        _logger.LogInformation("Leaving ecosystem {EcosystemId}", ecosystemId);
        await _hubConnection.InvokeAsync(nameof(ITerrariumHub.LeaveEcosystem), ecosystemId);
    }

    /// <summary>
    /// Sends a creature teleportation request to the hub.
    /// </summary>
    public async Task TeleportCreatureAsync(CreatureTeleport teleport)
    {
        _logger.LogInformation("Teleporting creature {OrganismId}", teleport.OrganismId);
        await _hubConnection.InvokeAsync(nameof(ITerrariumHub.TeleportCreature), teleport);
    }

    /// <summary>
    /// Announces peer presence and version info.
    /// </summary>
    public async Task AnnouncePeerAsync(PeerAnnounce announce)
    {
        await _hubConnection.InvokeAsync(nameof(ITerrariumHub.AnnouncePeer), announce);
    }

    /// <summary>
    /// Requests the current world state snapshot.
    /// </summary>
    public async Task<WorldStateUpdate> RequestWorldStateAsync(string ecosystemId)
    {
        return await _hubConnection.InvokeAsync<WorldStateUpdate>(
            nameof(ITerrariumHub.RequestWorldState), ecosystemId);
    }

    /// <summary>
    /// Sends a heartbeat to renew the peer lease. Call every 30 seconds.
    /// </summary>
    public async Task HeartbeatAsync(string ecosystemId)
    {
        _logger.LogDebug("Sending heartbeat for {EcosystemId}", ecosystemId);
        await _hubConnection.InvokeAsync(nameof(ITerrariumHub.Heartbeat), ecosystemId);
    }

    /// <summary>
    /// Requests the list of active peers in the ecosystem.
    /// </summary>
    public async Task<PeerListResponse> RequestPeerListAsync(string ecosystemId)
    {
        return await _hubConnection.InvokeAsync<PeerListResponse>(
            nameof(ITerrariumHub.RequestPeerList), ecosystemId);
    }

    /// <summary>
    /// Reports population statistics for the local ecosystem.
    /// </summary>
    public async Task ReportPopulationAsync(PopulationReport report)
    {
        _logger.LogDebug("Reporting population for {EcosystemId}", report.EcosystemId);
        await _hubConnection.InvokeAsync(nameof(ITerrariumHub.ReportPopulation), report);
    }

    // --- Cleanup ---

    public async ValueTask DisposeAsync()
    {
        _logger.LogInformation("Disposing TerrariumHubClient");
        await _hubConnection.DisposeAsync();
    }

    /// <summary>
    /// Exponential backoff retry policy matching Heisenberg's architecture spec:
    /// immediate, 2s, 10s, 30s, 60s — then give up.
    /// </summary>
    private sealed class ExponentialBackoffRetryPolicy : IRetryPolicy
    {
        private static readonly TimeSpan?[] _retryDelays =
        [
            TimeSpan.Zero,
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(60),
        ];

        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            return retryContext.PreviousRetryCount < _retryDelays.Length
                ? _retryDelays[retryContext.PreviousRetryCount]
                : null;
        }
    }
}
