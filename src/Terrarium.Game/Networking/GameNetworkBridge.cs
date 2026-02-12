// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using OrganismBase;
using Terrarium.Net;

namespace Terrarium.Game.Networking;

/// <summary>
/// Bridges the GameEngine to the NetworkEngine (SignalR).
/// Handles teleportation outbound/inbound, peer tracking, and heartbeat.
/// </summary>
public sealed class GameNetworkBridge : IAsyncDisposable
{
    private readonly GameEngine _engine;
    private readonly INetworkEngine _network;
    private readonly ILogger<GameNetworkBridge> _logger;
    private readonly string _ecosystemId;

    public GameNetworkBridge(
        GameEngine engine,
        INetworkEngine network,
        string ecosystemId,
        ILogger<GameNetworkBridge> logger)
    {
        _engine = engine;
        _network = network;
        _ecosystemId = ecosystemId;
        _logger = logger;

        // Wire inbound events from network → engine
        _network.OnCreatureTeleported += HandleInboundTeleportAsync;
        _network.OnPeerAnnounced += HandlePeerAnnouncedAsync;
    }

    /// <summary>
    /// Sends an organism to another peer via SignalR teleport.
    /// Called by the engine when an organism enters a teleport zone.
    /// </summary>
    public bool SendTeleport(OrganismState organismState, string? targetPeerId = null)
    {
        var species = (Species)organismState.Species;

        var teleport = new CreatureTeleport
        {
            TeleportId = Guid.NewGuid().ToString("N"),
            EcosystemId = _ecosystemId,
            OrganismId = organismState.ID,
            SpeciesAssemblyName = species.AssemblyFullName,
            SourcePeerId = "", // Set by hub from connection context
            TargetPeerId = targetPeerId,
            StatePayload = JsonSerializer.Serialize(new TeleportStatePayload
            {
                OrganismId = organismState.ID,
                SpeciesName = species.Name,
                Position = new PointData(organismState.Position.X, organismState.Position.Y),
                Radius = organismState.Radius,
                Generation = organismState.Generation,
                StoredEnergy = organismState.StoredEnergy,
                IsPlant = organismState is PlantState
            })
        };

        var enqueued = _network.EnqueueTeleport(teleport);
        if (enqueued)
        {
            _logger.LogInformation("Teleport sent: organism {Id} ({Species})",
                organismState.ID, species.Name);
        }
        return enqueued;
    }

    /// <summary>
    /// Handles inbound teleported creatures from the network and queues them for insertion.
    /// </summary>
    private Task HandleInboundTeleportAsync(CreatureTeleport teleport)
    {
        try
        {
            _logger.LogInformation("Teleport received: organism {Id} from peer {Peer}",
                teleport.OrganismId, teleport.SourcePeerId);

            // Queue the teleported creature for insertion into the world state
            _engine.OnTeleportReceived(teleport);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process inbound teleport {Id}", teleport.TeleportId);
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles peer join/leave events and updates engine state.
    /// </summary>
    private Task HandlePeerAnnouncedAsync(PeerAnnounce announce)
    {
        switch (announce.Action)
        {
            case PeerAction.Join:
                _logger.LogInformation("Peer joined: {PeerId}", announce.PeerId);
                _engine.OnEngineStateChanged(new EngineStateChangedEventArgs(
                    $"Peer {announce.PeerId[..Math.Min(8, announce.PeerId.Length)]} joined the ecosystem"));
                break;

            case PeerAction.Leave:
                _logger.LogInformation("Peer left: {PeerId}", announce.PeerId);
                _engine.OnEngineStateChanged(new EngineStateChangedEventArgs(
                    $"Peer {announce.PeerId[..Math.Min(8, announce.PeerId.Length)]} left the ecosystem"));
                break;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the current active peer count (excluding blacklisted).
    /// </summary>
    public int PeerCount => _network.Peers.Count;

    /// <summary>
    /// Whether the network connection is active.
    /// </summary>
    public bool IsConnected => _network.IsConnected;

    public ValueTask DisposeAsync()
    {
        _network.OnCreatureTeleported -= HandleInboundTeleportAsync;
        _network.OnPeerAnnounced -= HandlePeerAnnouncedAsync;
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Serializable teleport state payload for organism data transfer.
/// </summary>
public sealed class TeleportStatePayload
{
    public required string OrganismId { get; init; }
    public required string SpeciesName { get; init; }
    public required PointData Position { get; init; }
    public int Radius { get; init; }
    public int Generation { get; init; }
    public double StoredEnergy { get; init; }
    public bool IsPlant { get; init; }
}

/// <summary>
/// Serializable point data (System.Drawing.Point is not JSON-friendly).
/// </summary>
public sealed record PointData(int X, int Y);
