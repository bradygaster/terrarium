using Microsoft.Extensions.Logging;
using Terrarium.Orleans.Models;

namespace Terrarium.Orleans.Grains;

/// <summary>
/// Orleans grain that records population snapshots for trend analysis.
/// Maintains a bounded history per ecosystem.
/// </summary>
public class PopulationGrain : Grain, IPopulationGrain
{
    private readonly ILogger<PopulationGrain> _logger;
    private readonly List<PopulationSnapshot> _history = new();

    private const int MaxHistory = 1000;

    public PopulationGrain(ILogger<PopulationGrain> logger)
    {
        _logger = logger;
    }

    public Task RecordSnapshotAsync(PopulationSnapshot snapshot)
    {
        _history.Add(snapshot);

        if (_history.Count > MaxHistory)
        {
            _history.RemoveRange(0, _history.Count - MaxHistory);
        }

        _logger.LogDebug("Population snapshot recorded for ecosystem {Id} at tick {Tick}: {Count} organisms",
            this.GetPrimaryKeyString(), snapshot.TickNumber, snapshot.TotalOrganisms);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PopulationSnapshot>> GetHistoryAsync(int limit)
    {
        var result = _history
            .OrderByDescending(s => s.TickNumber)
            .Take(limit)
            .ToList();

        return Task.FromResult<IReadOnlyList<PopulationSnapshot>>(result);
    }
}
