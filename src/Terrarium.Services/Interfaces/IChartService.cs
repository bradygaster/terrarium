using Terrarium.Services.Models;

namespace Terrarium.Services.Interfaces;

public interface IChartService
{
    Task<IReadOnlyList<SpeciesInfo>> GetSpeciesListAsync(CancellationToken cancellationToken = default);

    Task<string> ChartPopulationAsync(DateTime start, DateTime end, IReadOnlyList<string> speciesNames,
        CancellationToken cancellationToken = default);

    Task<string> ChartVitalsAsync(DateTime start, DateTime end, string species,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpeciesInfo>> GrabLatestSpeciesDataAsync(string species,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SpeciesInfo>> GetTopAnimalsAsync(string version, OrganismType type, int count,
        CancellationToken cancellationToken = default);
}
