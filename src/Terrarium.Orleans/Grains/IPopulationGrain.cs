using Terrarium.Orleans.Models;

namespace Terrarium.Orleans.Grains;

/// <summary>
/// Tracks population snapshots over time for an ecosystem.
/// Keyed by ecosystem ID (string).
/// </summary>
public interface IPopulationGrain : IGrainWithStringKey
{
    Task RecordSnapshotAsync(PopulationSnapshot snapshot);
    Task<IReadOnlyList<PopulationSnapshot>> GetHistoryAsync(int limit);
}
