using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Terrarium.Net;

namespace Terrarium.Server.Services;

/// <summary>
/// Aggregates population data from all connected clients across ecosystems.
/// Maintains in-memory snapshots for real-time broadcasting via SignalR.
/// Sprint 11: In-memory aggregation — will be replaced by Orleans grain in Sprint 12.
/// </summary>
public sealed class PopulationTrackingService : IPopulationTrackingService
{
    private readonly ILogger<PopulationTrackingService> _logger;
    
    // Ecosystem → (Species → AggregateStats)
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, SpeciesAggregate>> _ecosystemData = new();
    
    // Ecosystem → last broadcast tick
    private readonly ConcurrentDictionary<string, long> _lastBroadcastTick = new();
    
    // Minimum ticks between broadcasts to avoid spam (10 ticks = ~10 seconds at 1 tick/sec)
    private const long BroadcastThrottleInterval = 10;

    public PopulationTrackingService(ILogger<PopulationTrackingService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Records a population report from a client. Updates the aggregate data for the ecosystem.
    /// </summary>
    public void RecordReport(PopulationReport report, string connectionId)
    {
        var ecosystemData = _ecosystemData.GetOrAdd(report.EcosystemId, _ => new ConcurrentDictionary<string, SpeciesAggregate>());
        
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["PeerId"] = connectionId,
            ["EcosystemId"] = report.EcosystemId,
            ["TickNumber"] = report.TickNumber
        }))
        {
            foreach (var species in report.Species)
            {
                var aggregate = ecosystemData.GetOrAdd(species.SpeciesName, _ => new SpeciesAggregate
                {
                    SpeciesName = species.SpeciesName
                });
                
                aggregate.UpdateFromPeerReport(connectionId, species.Population, species.Births, species.Deaths);
            }
            
            _logger.LogDebug(
                "Recorded population report from {PeerId} for {EcosystemId} at tick {TickNumber}: {SpeciesCount} species",
                connectionId, report.EcosystemId, report.TickNumber, report.Species.Count);
        }
    }

    /// <summary>
    /// Removes a peer's contribution from all ecosystems when they disconnect.
    /// </summary>
    public void RemovePeer(string connectionId, string ecosystemId)
    {
        if (_ecosystemData.TryGetValue(ecosystemId, out var ecosystemData))
        {
            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["PeerId"] = connectionId,
                ["EcosystemId"] = ecosystemId
            }))
            {
                foreach (var aggregate in ecosystemData.Values)
                {
                    aggregate.RemovePeer(connectionId);
                }
                
                _logger.LogDebug("Removed peer {PeerId} from ecosystem {EcosystemId}", connectionId, ecosystemId);
            }
        }
    }

    /// <summary>
    /// Gets the current aggregated population report for an ecosystem.
    /// </summary>
    public PopulationReport? GetAggregateReport(string ecosystemId, long currentTick)
    {
        if (!_ecosystemData.TryGetValue(ecosystemId, out var ecosystemData) || ecosystemData.IsEmpty)
            return null;

        var speciesList = ecosystemData.Values
            .Select(agg => new SpeciesPopulation
            {
                SpeciesName = agg.SpeciesName,
                Population = agg.TotalPopulation,
                Births = agg.TotalBirths,
                Deaths = agg.TotalDeaths
            })
            .Where(s => s.Population > 0) // Filter out extinct species
            .ToList();

        if (speciesList.Count == 0)
            return null;

        var totalOrganisms = speciesList.Sum(s => s.Population);

        return new PopulationReport
        {
            EcosystemId = ecosystemId,
            TickNumber = currentTick,
            Species = speciesList,
            TotalOrganisms = totalOrganisms,
            Timestamp = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Checks if enough time has passed since the last broadcast to send a new one.
    /// Updates the last broadcast tick if true.
    /// </summary>
    public bool ShouldBroadcast(string ecosystemId, long currentTick)
    {
        var lastTick = _lastBroadcastTick.GetOrAdd(ecosystemId, -1);
        
        if (currentTick - lastTick >= BroadcastThrottleInterval)
        {
            _lastBroadcastTick[ecosystemId] = currentTick;
            return true;
        }
        
        return false;
    }

    /// <summary>
    /// Gets the total count of active species across all ecosystems.
    /// Used for metrics reporting.
    /// </summary>
    public int GetActiveSpeciesCount()
    {
        var totalSpecies = 0;
        foreach (var ecosystemData in _ecosystemData.Values)
        {
            // Count species with non-zero population
            totalSpecies += ecosystemData.Values.Count(agg => agg.TotalPopulation > 0);
        }
        return totalSpecies;
    }

    /// <summary>
    /// Clears stale ecosystem data for ecosystems with no active peers.
    /// Called periodically to prevent memory leaks.
    /// </summary>
    public void CleanupStaleEcosystems(TimeSpan maxAge)
    {
        var cutoff = DateTimeOffset.UtcNow - maxAge;
        var staleEcosystems = new List<string>();

        foreach (var (ecosystemId, data) in _ecosystemData)
        {
            var hasRecentData = data.Values.Any(agg => agg.LastUpdateTime > cutoff);
            if (!hasRecentData)
            {
                staleEcosystems.Add(ecosystemId);
            }
        }

        foreach (var ecosystemId in staleEcosystems)
        {
            if (_ecosystemData.TryRemove(ecosystemId, out _))
            {
                _lastBroadcastTick.TryRemove(ecosystemId, out _);
                
                using (_logger.BeginScope(new Dictionary<string, object>
                {
                    ["EcosystemId"] = ecosystemId
                }))
                {
                    _logger.LogInformation("Cleaned up stale ecosystem {EcosystemId}", ecosystemId);
                }
            }
        }
    }
}

/// <summary>
/// Aggregated population statistics for a single species across all connected peers.
/// Thread-safe via ConcurrentDictionary for peer contributions.
/// </summary>
internal sealed class SpeciesAggregate
{
    private readonly ConcurrentDictionary<string, PeerContribution> _peerContributions = new();

    public required string SpeciesName { get; init; }
    
    public int TotalPopulation => _peerContributions.Values.Sum(p => p.Population);
    public int TotalBirths => _peerContributions.Values.Sum(p => p.Births);
    public int TotalDeaths => _peerContributions.Values.Sum(p => p.Deaths);
    public DateTimeOffset LastUpdateTime { get; private set; } = DateTimeOffset.UtcNow;

    public void UpdateFromPeerReport(string peerId, int population, int births, int deaths)
    {
        var contribution = _peerContributions.GetOrAdd(peerId, _ => new PeerContribution());
        contribution.Population = population;
        contribution.Births = births;
        contribution.Deaths = deaths;
        contribution.LastReported = DateTimeOffset.UtcNow;
        LastUpdateTime = DateTimeOffset.UtcNow;
    }

    public void RemovePeer(string peerId)
    {
        _peerContributions.TryRemove(peerId, out _);
    }

    private sealed class PeerContribution
    {
        public int Population { get; set; }
        public int Births { get; set; }
        public int Deaths { get; set; }
        public DateTimeOffset LastReported { get; set; } = DateTimeOffset.UtcNow;
    }
}
