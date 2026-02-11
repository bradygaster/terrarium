using Terrarium.Services.Models;

namespace Terrarium.Services.Interfaces;

public interface IChartService
{
    Task<IReadOnlyList<PopulationHistoryEntry>> GetPopulationHistoryAsync(string species,
        DateTime? beginDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpeciesDistributionEntry>> GetSpeciesDistributionAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TopCreatureEntry>> GetTopCreaturesAsync(string version,
        string? type = null, int? count = null,
        CancellationToken cancellationToken = default);
}