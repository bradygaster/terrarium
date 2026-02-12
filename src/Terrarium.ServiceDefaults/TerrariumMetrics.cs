using System.Diagnostics.Metrics;

namespace Terrarium.ServiceDefaults;

/// <summary>
/// Server-specific metrics for monitoring Terrarium server operations.
/// Tracks connected peers, active species, population reports, SignalR connections,
/// teleportation events, and assembly uploads.
/// </summary>
public sealed class TerrariumMetrics : IDisposable
{
    private readonly Meter _meter;
    private readonly Func<int> _connectedPeerCountProvider;
    private readonly Func<int> _activeSpeciesCountProvider;

    /// <summary>
    /// Number of currently connected peers (gauge).
    /// </summary>
    public ObservableGauge<int> ConnectedPeerCount { get; }

    /// <summary>
    /// Number of active species across all ecosystems (gauge).
    /// </summary>
    public ObservableGauge<int> ActiveSpeciesCount { get; }

    /// <summary>
    /// Total population reports received from clients (counter).
    /// </summary>
    public Counter<long> PopulationReportsReceived { get; }

    /// <summary>
    /// Number of active SignalR connections (gauge).
    /// Same as ConnectedPeerCount, but named for SignalR-specific monitoring.
    /// </summary>
    public ObservableGauge<int> SignalRConnections { get; }

    /// <summary>
    /// Total teleportation events processed (counter).
    /// </summary>
    public Counter<long> TeleportationEvents { get; }

    /// <summary>
    /// Total assembly uploads received (counter).
    /// </summary>
    public Counter<long> AssemblyUploads { get; }

    public TerrariumMetrics(
        IMeterFactory meterFactory,
        Func<int>? connectedPeerCountProvider = null,
        Func<int>? activeSpeciesCountProvider = null)
    {
        _meter = meterFactory.Create("Terrarium.Server");
        _connectedPeerCountProvider = connectedPeerCountProvider ?? (() => 0);
        _activeSpeciesCountProvider = activeSpeciesCountProvider ?? (() => 0);

        // Gauges — observable counters that report current state
        ConnectedPeerCount = _meter.CreateObservableGauge(
            "terrarium.server.peers.connected",
            () => _connectedPeerCountProvider(),
            unit: "{peers}",
            description: "Number of currently connected peers");

        ActiveSpeciesCount = _meter.CreateObservableGauge(
            "terrarium.server.species.active",
            () => _activeSpeciesCountProvider(),
            unit: "{species}",
            description: "Number of active species across all ecosystems");

        SignalRConnections = _meter.CreateObservableGauge(
            "terrarium.server.signalr.connections",
            () => _connectedPeerCountProvider(),
            unit: "{connections}",
            description: "Number of active SignalR connections");

        // Counters — cumulative totals
        PopulationReportsReceived = _meter.CreateCounter<long>(
            "terrarium.server.population_reports.received",
            unit: "{reports}",
            description: "Total population reports received from clients");

        TeleportationEvents = _meter.CreateCounter<long>(
            "terrarium.server.teleportation.events",
            unit: "{events}",
            description: "Total teleportation events processed");

        AssemblyUploads = _meter.CreateCounter<long>(
            "terrarium.server.assemblies.uploads",
            unit: "{uploads}",
            description: "Total assembly uploads received");
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
