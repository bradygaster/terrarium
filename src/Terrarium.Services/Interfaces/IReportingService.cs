using Terrarium.Services.Models;

namespace Terrarium.Services.Interfaces;

public interface IReportingService
{
    Task<bool> ReportBugAsync(BugReport report, CancellationToken cancellationToken = default);

    Task<ReportPopulationResult> ReportPopulationAsync(Guid peerGuid, int currentTick,
        IReadOnlyList<PopulationHistoryRow> history,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpeciesWithPopulation>> GetSpeciesListAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpeciesPopulationData>> GetLatestSpeciesDataAsync(string species,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopAnimalEntry>> GetTopAnimalsAsync(string version, string type, int? count,
        CancellationToken cancellationToken = default);
}