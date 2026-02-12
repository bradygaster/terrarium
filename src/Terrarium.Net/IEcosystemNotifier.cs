namespace Terrarium.Net;

/// <summary>
/// Abstraction for pushing notifications from Orleans grains to SignalR clients.
/// Implemented in Terrarium.Server using IHubContext. This keeps Terrarium.Orleans
/// decoupled from Microsoft.AspNetCore.SignalR.
/// </summary>
public interface IEcosystemNotifier
{
    /// <summary>
    /// Pushes a tick notification to all clients in an ecosystem group.
    /// </summary>
    Task NotifyTickAsync(string ecosystemId, EcosystemTick tick);

    /// <summary>
    /// Pushes a world state update to all clients in an ecosystem group.
    /// </summary>
    Task NotifyWorldStateAsync(string ecosystemId, WorldStateUpdate update);

    /// <summary>
    /// Delivers a teleported creature to a specific client connection.
    /// </summary>
    Task NotifyTeleportAsync(string targetConnectionId, CreatureTeleport teleport);

    /// <summary>
    /// Pushes a population report to all clients in an ecosystem group.
    /// </summary>
    Task NotifyPopulationAsync(string ecosystemId, PopulationReport report);
}
