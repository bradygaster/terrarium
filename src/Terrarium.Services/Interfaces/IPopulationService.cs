using Terrarium.Services.Models;

namespace Terrarium.Services.Interfaces;

public interface IPopulationService
{
    Task<int> ReportPopulationAsync(PopulationData data, CancellationToken cancellationToken = default);
}
