using Terrarium.Services.Models;

namespace Terrarium.Services.Interfaces;

public interface IPopulationService
{
    Task<ReportPopulationResult> ReportPopulationAsync(Guid peerGuid, int currentTick,
        IReadOnlyList<PopulationHistoryRow> history,
        CancellationToken cancellationToken = default);
}