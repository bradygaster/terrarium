using System.Net.Http.Json;
using Terrarium.Services.Interfaces;
using Terrarium.Services.Models;

namespace Terrarium.Services.Clients;

public sealed class PopulationServiceClient(HttpClient httpClient) : IPopulationService
{
    public async Task<ReportPopulationResult> ReportPopulationAsync(Guid peerGuid, int currentTick,
        IReadOnlyList<PopulationHistoryRow> history,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("reporting/population",
            new { guid = peerGuid, currentTick, history }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ReportPopulationResult>(cancellationToken)
            ?? new ReportPopulationResult { ReturnCode = ReportingReturnCode.ServerDown };
    }
}