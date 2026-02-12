// Copyright (c) Microsoft Corporation.  All rights reserved.

using Terrarium.Net;

namespace Terrarium.Game.Networking;

/// <summary>
/// Abstraction over the networking engine for DI and testability.
/// </summary>
public interface INetworkEngine : IAsyncDisposable
{
    bool IsConnected { get; }
    IReadOnlyDictionary<string, PeerRecord> Peers { get; }
    IReadOnlyDictionary<string, DateTimeOffset> Blacklist { get; }

    event Func<CreatureTeleport, Task>? OnCreatureTeleported;
    event Func<PeerAnnounce, Task>? OnPeerAnnounced;
    event Func<PeerListResponse, Task>? OnPeerListReceived;
    event Func<PopulationReport, Task>? OnPopulationReported;
    event Func<EcosystemTick, Task>? OnEcosystemTick;
    event Func<WorldStateUpdate, Task>? OnWorldStateUpdated;
    event Func<HubError, Task>? OnError;

    Task ConnectAsync(CancellationToken cancellationToken = default);
    Task JoinEcosystemAsync(string ecosystemId);
    Task LeaveEcosystemAsync();
    bool EnqueueTeleport(CreatureTeleport teleport);
    bool EnqueuePopulationReport(PopulationReport report);
    bool RequestPeerList();
    void BlacklistPeer(string peerId, string reason);
}
