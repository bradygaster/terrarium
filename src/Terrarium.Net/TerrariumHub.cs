using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Terrarium.ServiceDefaults;

namespace Terrarium.Net;

/// <summary>
/// SignalR hub for real-time Terrarium ecosystem communication.
/// Uses in-memory ConcurrentDictionary state until Orleans grain integration in Sprint 11.
/// Hub never throws — all errors go through ReceiveError callback per Heisenberg's design.
/// </summary>
public class TerrariumHub : Hub<ITerrariumClient>, ITerrariumHub
{
    private readonly ILogger<TerrariumHub> _logger;
    private readonly IPopulationTrackingService? _populationTracking;
    private readonly TerrariumMetrics? _metrics;

    // In-memory peer state — keyed by connectionId → PeerState
    private static readonly ConcurrentDictionary<string, PeerState> s_peers = new();

    // Per-connection rate limiting — keyed by connectionId → (method → sliding window)
    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<string, RateLimitWindow>> s_rateLimits = new();

    public TerrariumHub(
        ILogger<TerrariumHub> logger,
        IPopulationTrackingService? populationTracking = null,
        TerrariumMetrics? metrics = null)
    {
        _logger = logger;
        _populationTracking = populationTracking;
        _metrics = metrics;
    }

    /// <summary>
    /// Gets the current count of connected peers across all ecosystems.
    /// Used by metrics provider.
    /// </summary>
    public static int GetConnectedPeerCount() => s_peers.Count;

    public async Task JoinEcosystem(string ecosystemId)
    {
        var connId = Context.ConnectionId;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["PeerId"] = connId,
            ["EcosystemId"] = ecosystemId
        }))
        {
            var peerState = new PeerState
            {
                ConnectionId = connId,
                EcosystemId = ecosystemId,
                ConnectedAt = DateTimeOffset.UtcNow,
                LastHeartbeat = DateTimeOffset.UtcNow
            };
            s_peers[connId] = peerState;

            await Groups.AddToGroupAsync(connId, $"ecosystem-{ecosystemId}");
            _logger.LogInformation("Peer {PeerId} joined ecosystem {EcosystemId}", connId, ecosystemId);

            var announce = new PeerAnnounce
            {
                PeerId = connId,
                EcosystemId = ecosystemId,
                Action = PeerAction.Join
            };

            await Clients.OthersInGroup($"ecosystem-{ecosystemId}").ReceivePeerAnnounce(announce);
        }
    }

    public async Task LeaveEcosystem(string ecosystemId)
    {
        var connId = Context.ConnectionId;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["PeerId"] = connId,
            ["EcosystemId"] = ecosystemId
        }))
        {
            s_peers.TryRemove(connId, out _);
            s_rateLimits.TryRemove(connId, out _);

            await Groups.RemoveFromGroupAsync(connId, $"ecosystem-{ecosystemId}");
            _logger.LogInformation("Peer {PeerId} left ecosystem {EcosystemId}", connId, ecosystemId);

            var announce = new PeerAnnounce
            {
                PeerId = connId,
                EcosystemId = ecosystemId,
                Action = PeerAction.Leave
            };

            await Clients.OthersInGroup($"ecosystem-{ecosystemId}").ReceivePeerAnnounce(announce);
        }
    }

    public async Task TeleportCreature(CreatureTeleport teleport)
    {
        if (IsRateLimited(nameof(TeleportCreature), 10, 60))
            return;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["PeerId"] = Context.ConnectionId,
            ["EcosystemId"] = teleport.EcosystemId,
            ["TeleportId"] = teleport.TeleportId,
            ["OrganismId"] = teleport.OrganismId
        }))
        {
            _logger.LogInformation(
                "Teleport {TeleportId}: organism {OrganismId} from {SourcePeerId} in ecosystem {EcosystemId}",
                teleport.TeleportId, teleport.OrganismId, teleport.SourcePeerId, teleport.EcosystemId);

            // Increment teleportation metrics
            _metrics?.TeleportationEvents.Add(1, new KeyValuePair<string, object?>("ecosystem_id", teleport.EcosystemId));

            // TODO: Sprint 11 — delegate to SpeciesRegistryGrain for species validation
            // TODO: Sprint 11 — delegate to EcosystemGrain.RemoveOrganismAsync

            if (teleport.TargetPeerId is not null)
            {
                if (!s_peers.ContainsKey(teleport.TargetPeerId))
                {
                    await Clients.Caller.ReceiveError(new HubError
                    {
                        Code = "TELEPORT_FAILED",
                        Message = $"Target peer {teleport.TargetPeerId} is not connected",
                        IsTransient = true,
                        CorrelationId = teleport.TeleportId
                    });
                    return;
                }
                await Clients.Client(teleport.TargetPeerId).ReceiveCreatureTeleport(teleport);
            }
            else
            {
                // Pick a random connected peer in the same ecosystem (excluding sender)
                var candidates = s_peers.Values
                    .Where(p => p.EcosystemId == teleport.EcosystemId && p.ConnectionId != Context.ConnectionId)
                    .ToList();

                if (candidates.Count == 0)
                {
                    await Clients.Caller.ReceiveError(new HubError
                    {
                        Code = "TELEPORT_FAILED",
                        Message = "No peers available in ecosystem for teleport",
                        IsTransient = true,
                        CorrelationId = teleport.TeleportId
                    });
                    return;
                }

                var target = candidates[Random.Shared.Next(candidates.Count)];
                await Clients.Client(target.ConnectionId).ReceiveCreatureTeleport(teleport);
            }
        }
    }

    public async Task AnnouncePeer(PeerAnnounce announce)
    {
        _logger.LogInformation(
            "Peer announce: {PeerId} {Action} in {EcosystemId}",
            announce.PeerId, announce.Action, announce.EcosystemId);

        if (s_peers.TryGetValue(Context.ConnectionId, out var state))
        {
            state.Version = announce.Version;
            state.Channel = announce.Channel;
        }

        // TODO: Sprint 11 — register with PeerGrain for lease management
        await Clients.OthersInGroup($"ecosystem-{announce.EcosystemId}").ReceivePeerAnnounce(announce);
    }

    public async Task<WorldStateUpdate> RequestWorldState(string ecosystemId)
    {
        if (IsRateLimited(nameof(RequestWorldState), 5, 60))
        {
            return new WorldStateUpdate
            {
                EcosystemId = ecosystemId,
                TickNumber = -1
            };
        }

        _logger.LogInformation("World state requested by {ConnectionId} for {EcosystemId}", Context.ConnectionId, ecosystemId);

        var peerCount = s_peers.Values.Count(p => p.EcosystemId == ecosystemId);

        // TODO: Sprint 11 — fetch from EcosystemGrain.GetWorldStateAsync
        return await Task.FromResult(new WorldStateUpdate
        {
            EcosystemId = ecosystemId,
            TickNumber = 0,
            WorldWidth = 0,
            WorldHeight = 0,
            OrganismCount = peerCount
        });
    }

    public async Task Heartbeat(string ecosystemId)
    {
        if (IsRateLimited(nameof(Heartbeat), 3, 60))
            return;

        var connId = Context.ConnectionId;
        if (s_peers.TryGetValue(connId, out var state))
        {
            state.LastHeartbeat = DateTimeOffset.UtcNow;
        }

        _logger.LogDebug("Heartbeat from {ConnectionId} in {EcosystemId}", connId, ecosystemId);

        // TODO: Sprint 11 — delegate to PeerGrain.HeartbeatAsync()
        await Task.CompletedTask;
    }

    public async Task<PeerListResponse> RequestPeerList(string ecosystemId)
    {
        if (IsRateLimited(nameof(RequestPeerList), 2, 60))
        {
            return new PeerListResponse { EcosystemId = ecosystemId, Peers = [] };
        }

        _logger.LogInformation("Peer list requested by {ConnectionId} for {EcosystemId}", Context.ConnectionId, ecosystemId);

        var peers = s_peers.Values
            .Where(p => p.EcosystemId == ecosystemId)
            .Select(p => new PeerInfo
            {
                PeerId = p.ConnectionId,
                Version = p.Version,
                ConnectedAt = p.ConnectedAt
            })
            .ToList();

        // TODO: Sprint 11 — enumerate from EcosystemGrain / PeerGrain
        return await Task.FromResult(new PeerListResponse
        {
            EcosystemId = ecosystemId,
            Peers = peers
        });
    }

    public async Task ReportPopulation(PopulationReport report)
    {
        if (IsRateLimited(nameof(ReportPopulation), 2, 60))
            return;

        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["PeerId"] = Context.ConnectionId,
            ["EcosystemId"] = report.EcosystemId,
            ["TickNumber"] = report.TickNumber
        }))
        {
            _logger.LogInformation(
                "Population report from {PeerId} for {EcosystemId}: {TotalOrganisms} organisms, {SpeciesCount} species",
                Context.ConnectionId, report.EcosystemId, report.TotalOrganisms, report.Species.Count);

            // Increment population report metrics
            _metrics?.PopulationReportsReceived.Add(1, new KeyValuePair<string, object?>("ecosystem_id", report.EcosystemId));

            // Record this peer's contribution in the aggregation service
            _populationTracking?.RecordReport(report, Context.ConnectionId);

            // Get the current aggregate report for this ecosystem
            var aggregateReport = _populationTracking?.GetAggregateReport(report.EcosystemId, report.TickNumber);

            // Broadcast aggregate data if throttle allows and we have data
            if (aggregateReport is not null && (_populationTracking?.ShouldBroadcast(report.EcosystemId, report.TickNumber) ?? false))
            {
                _logger.LogInformation(
                    "Broadcasting aggregate population for {EcosystemId}: {TotalOrganisms} total organisms across {SpeciesCount} species",
                    report.EcosystemId, aggregateReport.TotalOrganisms, aggregateReport.Species.Count);

                await Clients.Group($"ecosystem-{report.EcosystemId}").ReceivePopulationReport(aggregateReport);
            }
            else
            {
                // If we don't have aggregate tracking, fall back to old behavior: relay individual reports
                await Clients.OthersInGroup($"ecosystem-{report.EcosystemId}").ReceivePopulationReport(report);
            }
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connId = Context.ConnectionId;

        if (s_peers.TryRemove(connId, out var state))
        {
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["PeerId"] = connId,
                ["EcosystemId"] = state.EcosystemId
            }))
            {
                if (exception != null)
                {
                    _logger.LogWarning(exception, "Peer {PeerId} disconnected with error", connId);
                }
                else
                {
                    _logger.LogInformation("Peer {PeerId} disconnected", connId);
                }

                // Remove peer from population tracking
                _populationTracking?.RemovePeer(connId, state.EcosystemId);

                var announce = new PeerAnnounce
                {
                    PeerId = connId,
                    EcosystemId = state.EcosystemId,
                    Action = PeerAction.Leave
                };
                await Clients.OthersInGroup($"ecosystem-{state.EcosystemId}").ReceivePeerAnnounce(announce);
            }
        }
        else
        {
            _logger.LogDebug("Peer {PeerId} disconnected (no state found)", connId);
        }

        s_rateLimits.TryRemove(connId, out _);
        // TODO: Sprint 11 — notify PeerGrain to revoke lease
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Checks per-connection rate limits. If exceeded, sends ReceiveError and returns true.
    /// </summary>
    private bool IsRateLimited(string method, int maxCalls, int windowSeconds)
    {
        var connId = Context.ConnectionId;
        var windows = s_rateLimits.GetOrAdd(connId, _ => new ConcurrentDictionary<string, RateLimitWindow>());
        var window = windows.GetOrAdd(method, _ => new RateLimitWindow());

        var now = DateTimeOffset.UtcNow;
        lock (window)
        {
            window.Timestamps.RemoveAll(t => (now - t).TotalSeconds > windowSeconds);
            if (window.Timestamps.Count >= maxCalls)
            {
                var oldest = window.Timestamps[0];
                var retryAfterMs = (int)((oldest.AddSeconds(windowSeconds) - now).TotalMilliseconds);

                _ = Clients.Caller.ReceiveError(new HubError
                {
                    Code = "RATE_LIMITED",
                    Message = $"{method} rate limit exceeded ({maxCalls}/{windowSeconds}s)",
                    IsTransient = true,
                    RetryAfterMs = Math.Max(retryAfterMs, 1000)
                });

                _logger.LogWarning("Rate limited {ConnectionId} on {Method}", connId, method);
                return true;
            }
            window.Timestamps.Add(now);
        }
        return false;
    }

    /// <summary>
    /// In-memory peer tracking state. Replaced by PeerGrain in Sprint 11.
    /// </summary>
    internal sealed class PeerState
    {
        public required string ConnectionId { get; init; }
        public required string EcosystemId { get; init; }
        public DateTimeOffset ConnectedAt { get; init; }
        public DateTimeOffset LastHeartbeat { get; set; }
        public string? Version { get; set; }
        public string? Channel { get; set; }
    }

    private sealed class RateLimitWindow
    {
        public List<DateTimeOffset> Timestamps { get; } = [];
    }
}
