namespace Terrarium.Net;

/// <summary>
/// Service for tracking and aggregating population data across all connected clients.
/// Implemented by the server-side PopulationTrackingService.
/// </summary>
public interface IPopulationTrackingService
{
    /// <summary>
    /// Records a population report from a client and updates aggregate data.
    /// </summary>
    void RecordReport(PopulationReport report, string connectionId);

    /// <summary>
    /// Removes a peer's contribution when they disconnect.
    /// </summary>
    void RemovePeer(string connectionId, string ecosystemId);

    /// <summary>
    /// Gets the current aggregated population report for an ecosystem.
    /// </summary>
    PopulationReport? GetAggregateReport(string ecosystemId, long currentTick);

    /// <summary>
    /// Checks if enough time has passed to broadcast a new aggregate report.
    /// </summary>
    bool ShouldBroadcast(string ecosystemId, long currentTick);

    /// <summary>
    /// Gets the total count of active species across all ecosystems.
    /// Used for metrics reporting.
    /// </summary>
    int GetActiveSpeciesCount();
}
