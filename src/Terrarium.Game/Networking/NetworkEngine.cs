using System.Collections.Concurrent;
using System.Threading.Channels;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Terrarium.Net;

namespace Terrarium.Game.Networking;

/// <summary>
/// Client-side networking abstraction replacing the legacy socket-based NetworkEngine.
/// Manages SignalR connection, peer tracking, rate limiting, and bad-peer blacklisting.
/// Uses async Channel&lt;T&gt; work queue for outbound operations.
/// </summary>
public sealed class NetworkEngine : INetworkEngine
{
    private readonly ILogger<NetworkEngine> _logger;
    private readonly NetworkEngineOptions _options;
    private HubConnection? _connection;
    private CancellationTokenSource? _cts;
    private Task? _workQueueTask;
    private Task? _heartbeatTask;

    // Peer tracking
    private readonly ConcurrentDictionary<string, PeerRecord> _peers = new();
    private readonly ConcurrentDictionary<string, DateTimeOffset> _blacklist = new();

    // Rate limiting: method → last invocation time
    private readonly ConcurrentDictionary<string, DateTimeOffset> _lastCallTimes = new();
    private static readonly TimeSpan ThrottleInterval = TimeSpan.FromSeconds(30);

    // Outbound work queue
    private readonly Channel<Func<HubConnection, CancellationToken, Task>> _workChannel =
        Channel.CreateBounded<Func<HubConnection, CancellationToken, Task>>(
            new BoundedChannelOptions(256)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true
            });

    private string? _currentEcosystemId;

    /// <summary>Raised when a creature teleport is received from another peer.</summary>
    public event Func<CreatureTeleport, Task>? OnCreatureTeleported;

    /// <summary>Raised when a peer joins or leaves the ecosystem.</summary>
    public event Func<PeerAnnounce, Task>? OnPeerAnnounced;

    /// <summary>Raised when the peer list is received.</summary>
    public event Func<PeerListResponse, Task>? OnPeerListReceived;

    /// <summary>Raised when a population report is received.</summary>
    public event Func<PopulationReport, Task>? OnPopulationReported;

    /// <summary>Raised when an ecosystem tick is received.</summary>
    public event Func<EcosystemTick, Task>? OnEcosystemTick;

    /// <summary>Raised when a world state update is received.</summary>
    public event Func<WorldStateUpdate, Task>? OnWorldStateUpdated;

    /// <summary>Raised when the hub sends an error.</summary>
    public event Func<HubError, Task>? OnError;

    /// <summary>Active peers in the current ecosystem (excludes blacklisted).</summary>
    public IReadOnlyDictionary<string, PeerRecord> Peers => _peers;

    /// <summary>Currently blacklisted peer IDs.</summary>
    public IReadOnlyDictionary<string, DateTimeOffset> Blacklist => _blacklist;

    /// <summary>Whether the connection is active.</summary>
    public bool IsConnected => _connection?.State == HubConnectionState.Connected;

    public NetworkEngine(ILogger<NetworkEngine> logger, NetworkEngineOptions options)
    {
        _logger = logger;
        _options = options;
    }

    /// <summary>
    /// Establishes the SignalR connection and starts the work queue processor.
    /// </summary>
    public async Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _connection = new HubConnectionBuilder()
            .WithUrl(_options.HubUrl)
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60) })
            .Build();

        RegisterCallbacks(_connection);

        _connection.Reconnected += async connectionId =>
        {
            _logger.LogInformation("Reconnected with new connectionId {ConnectionId}", connectionId);
            if (_currentEcosystemId is not null)
            {
                await _connection.InvokeAsync("JoinEcosystem", _currentEcosystemId, _cts.Token);
            }
        };

        _connection.Closed += ex =>
        {
            _logger.LogWarning(ex, "SignalR connection closed");
            return Task.CompletedTask;
        };

        await _connection.StartAsync(_cts.Token);
        _logger.LogInformation("Connected to hub at {Url}", _options.HubUrl);

        _workQueueTask = ProcessWorkQueueAsync(_cts.Token);
    }

    /// <summary>
    /// Joins an ecosystem and starts heartbeat.
    /// </summary>
    public async Task JoinEcosystemAsync(string ecosystemId)
    {
        if (_connection is null) throw new InvalidOperationException("Not connected");
        _currentEcosystemId = ecosystemId;
        await _connection.InvokeAsync("JoinEcosystem", ecosystemId);
        _heartbeatTask = RunHeartbeatAsync(_cts!.Token);
        _logger.LogInformation("Joined ecosystem {EcosystemId}", ecosystemId);
    }

    /// <summary>
    /// Leaves the current ecosystem.
    /// </summary>
    public async Task LeaveEcosystemAsync()
    {
        if (_connection is null || _currentEcosystemId is null) return;
        await _connection.InvokeAsync("LeaveEcosystem", _currentEcosystemId);
        _currentEcosystemId = null;
        _peers.Clear();
    }

    /// <summary>
    /// Enqueues a creature teleport. Respects 30-second throttle.
    /// </summary>
    public bool EnqueueTeleport(CreatureTeleport teleport)
    {
        if (IsThrottled(nameof(EnqueueTeleport))) return false;
        if (teleport.TargetPeerId is not null && _blacklist.ContainsKey(teleport.TargetPeerId))
        {
            _logger.LogWarning("Skipping teleport to blacklisted peer {PeerId}", teleport.TargetPeerId);
            return false;
        }

        return _workChannel.Writer.TryWrite(async (conn, ct) =>
            await conn.InvokeAsync("TeleportCreature", teleport, ct));
    }

    /// <summary>
    /// Enqueues a population report. Respects 30-second throttle.
    /// </summary>
    public bool EnqueuePopulationReport(PopulationReport report)
    {
        if (IsThrottled(nameof(EnqueuePopulationReport))) return false;
        return _workChannel.Writer.TryWrite(async (conn, ct) =>
            await conn.InvokeAsync("ReportPopulation", report, ct));
    }

    /// <summary>
    /// Requests the current peer list.
    /// </summary>
    public bool RequestPeerList()
    {
        if (_currentEcosystemId is null) return false;
        var ecoId = _currentEcosystemId;
        return _workChannel.Writer.TryWrite(async (conn, ct) =>
            await conn.InvokeAsync("RequestPeerList", ecoId, ct));
    }

    /// <summary>
    /// Adds a peer to the blacklist for the configured timeout (default 1 hour).
    /// </summary>
    public void BlacklistPeer(string peerId, string reason)
    {
        _blacklist[peerId] = DateTimeOffset.UtcNow;
        _peers.TryRemove(peerId, out _);
        _logger.LogWarning("Blacklisted peer {PeerId}: {Reason}", peerId, reason);
    }

    private void RegisterCallbacks(HubConnection connection)
    {
        connection.On<CreatureTeleport>("ReceiveCreatureTeleport", async teleport =>
        {
            if (_blacklist.ContainsKey(teleport.SourcePeerId))
            {
                _logger.LogDebug("Ignoring teleport from blacklisted peer {PeerId}", teleport.SourcePeerId);
                return;
            }
            if (OnCreatureTeleported is not null)
                await OnCreatureTeleported(teleport);
        });

        connection.On<PeerAnnounce>("ReceivePeerAnnounce", async announce =>
        {
            switch (announce.Action)
            {
                case PeerAction.Join:
                    _peers[announce.PeerId] = new PeerRecord
                    {
                        PeerId = announce.PeerId,
                        Version = announce.Version,
                        JoinedAt = announce.Timestamp
                    };
                    break;
                case PeerAction.Leave:
                    _peers.TryRemove(announce.PeerId, out _);
                    break;
            }
            if (OnPeerAnnounced is not null)
                await OnPeerAnnounced(announce);
        });

        connection.On<PeerListResponse>("ReceivePeerList", async peerList =>
        {
            foreach (var peer in peerList.Peers)
            {
                if (!_blacklist.ContainsKey(peer.PeerId))
                {
                    _peers[peer.PeerId] = new PeerRecord
                    {
                        PeerId = peer.PeerId,
                        Version = peer.Version,
                        JoinedAt = peer.ConnectedAt
                    };
                }
            }
            if (OnPeerListReceived is not null)
                await OnPeerListReceived(peerList);
        });

        connection.On<PopulationReport>("ReceivePopulationReport", async report =>
        {
            if (OnPopulationReported is not null)
                await OnPopulationReported(report);
        });

        connection.On<EcosystemTick>("ReceiveEcosystemTick", async tick =>
        {
            if (OnEcosystemTick is not null)
                await OnEcosystemTick(tick);
        });

        connection.On<WorldStateUpdate>("ReceiveWorldStateUpdate", async update =>
        {
            if (OnWorldStateUpdated is not null)
                await OnWorldStateUpdated(update);
        });

        connection.On<HubError>("ReceiveError", async error =>
        {
            _logger.LogWarning("Hub error [{Code}]: {Message}", error.Code, error.Message);
            if (OnError is not null)
                await OnError(error);
        });
    }

    private async Task ProcessWorkQueueAsync(CancellationToken ct)
    {
        try
        {
            await foreach (var workItem in _workChannel.Reader.ReadAllAsync(ct))
            {
                if (_connection is null || _connection.State != HubConnectionState.Connected)
                {
                    _logger.LogDebug("Skipping work item — not connected");
                    continue;
                }

                try
                {
                    await workItem(_connection, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Work item failed");
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    private async Task RunHeartbeatAsync(CancellationToken ct)
    {
        try
        {
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
            while (await timer.WaitForNextTickAsync(ct))
            {
                if (_connection?.State == HubConnectionState.Connected && _currentEcosystemId is not null)
                {
                    try
                    {
                        await _connection.InvokeAsync("Heartbeat", _currentEcosystemId, ct);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Heartbeat failed");
                    }
                }

                CleanupExpiredBlacklist();
            }
        }
        catch (OperationCanceledException) { }
    }

    private void CleanupExpiredBlacklist()
    {
        var expiry = DateTimeOffset.UtcNow - _options.BlacklistTimeout;
        foreach (var kvp in _blacklist)
        {
            if (kvp.Value < expiry)
                _blacklist.TryRemove(kvp.Key, out _);
        }
    }

    private bool IsThrottled(string method)
    {
        var now = DateTimeOffset.UtcNow;
        if (_lastCallTimes.TryGetValue(method, out var last) && (now - last) < ThrottleInterval)
        {
            _logger.LogDebug("Throttled: {Method}", method);
            return true;
        }
        _lastCallTimes[method] = now;
        return false;
    }

    public async ValueTask DisposeAsync()
    {
        _cts?.Cancel();
        _workChannel.Writer.TryComplete();

        if (_workQueueTask is not null)
            await _workQueueTask;
        if (_heartbeatTask is not null)
            await _heartbeatTask;
        if (_connection is not null)
            await _connection.DisposeAsync();

        _cts?.Dispose();
    }
}

/// <summary>
/// Configuration options for NetworkEngine.
/// </summary>
public sealed class NetworkEngineOptions
{
    /// <summary>URL of the TerrariumHub endpoint (e.g., "https://localhost:5001/hubs/terrarium").</summary>
    public required string HubUrl { get; init; }

    /// <summary>How long a blacklisted peer stays blacklisted. Default: 1 hour (matches legacy).</summary>
    public TimeSpan BlacklistTimeout { get; init; } = TimeSpan.FromHours(1);
}

/// <summary>
/// Tracks a connected peer's state on the client side.
/// </summary>
public sealed class PeerRecord
{
    public required string PeerId { get; init; }
    public string? Version { get; init; }
    public DateTimeOffset JoinedAt { get; init; }
}
