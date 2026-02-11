using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Terrarium.ServiceDefaults;

/// <summary>
/// Centralized OpenTelemetry instrumentation for Terrarium services.
/// Defines custom activity sources (traces) and metrics that flow to the Aspire dashboard.
/// </summary>
public sealed class TerrariumTelemetry : IDisposable
{
    /// <summary>Meter name used for all Terrarium custom metrics.</summary>
    public const string MeterName = "Terrarium";

    /// <summary>Activity source for creature registration operations.</summary>
    public const string CreatureRegistrationSource = "Terrarium.CreatureRegistration";

    /// <summary>Activity source for peer discovery operations.</summary>
    public const string PeerDiscoverySource = "Terrarium.PeerDiscovery";

    /// <summary>Activity source for teleportation operations.</summary>
    public const string TeleportationSource = "Terrarium.Teleportation";

    /// <summary>All custom activity source names, for OTel registration.</summary>
    public static readonly string[] ActivitySourceNames =
    [
        CreatureRegistrationSource,
        PeerDiscoverySource,
        TeleportationSource,
    ];

    private readonly Meter _meter;

    public ActivitySource CreatureRegistration { get; } = new(CreatureRegistrationSource);
    public ActivitySource PeerDiscovery { get; } = new(PeerDiscoverySource);
    public ActivitySource Teleportation { get; } = new(TeleportationSource);

    /// <summary>Total creatures currently alive in the ecosystem.</summary>
    public ObservableGauge<long> CreatureCount { get; }

    /// <summary>Number of ecosystem ticks processed.</summary>
    public Counter<long> TickCount { get; }

    /// <summary>Number of API requests handled.</summary>
    public Counter<long> ApiRequestCount { get; }

    /// <summary>Number of creature registrations.</summary>
    public Counter<long> CreatureRegistrations { get; }

    /// <summary>Number of teleportation events.</summary>
    public Counter<long> TeleportationEvents { get; }

    /// <summary>Number of peer discovery lookups.</summary>
    public Counter<long> PeerDiscoveryLookups { get; }

    public TerrariumTelemetry(IMeterFactory meterFactory, Func<long>? creatureCountProvider = null)
    {
        _meter = meterFactory.Create(MeterName);

        CreatureCount = _meter.CreateObservableGauge(
            "terrarium.creatures.count",
            creatureCountProvider ?? (() => 0),
            unit: "{creatures}",
            description: "Current number of creatures alive in the ecosystem");

        TickCount = _meter.CreateCounter<long>(
            "terrarium.ticks.count",
            unit: "{ticks}",
            description: "Total ecosystem ticks processed");

        ApiRequestCount = _meter.CreateCounter<long>(
            "terrarium.api.requests",
            unit: "{requests}",
            description: "Total API requests handled");

        CreatureRegistrations = _meter.CreateCounter<long>(
            "terrarium.creatures.registrations",
            unit: "{registrations}",
            description: "Total creature registrations");

        TeleportationEvents = _meter.CreateCounter<long>(
            "terrarium.teleportation.events",
            unit: "{events}",
            description: "Total teleportation events");

        PeerDiscoveryLookups = _meter.CreateCounter<long>(
            "terrarium.peers.lookups",
            unit: "{lookups}",
            description: "Total peer discovery lookups");
    }

    public void Dispose()
    {
        CreatureRegistration.Dispose();
        PeerDiscovery.Dispose();
        Teleportation.Dispose();
        _meter.Dispose();
    }
}
