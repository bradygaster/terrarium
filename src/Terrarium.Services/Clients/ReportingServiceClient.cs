using System.Net.Http.Json;
using Terrarium.Services.Interfaces;
using Terrarium.Services.Models;

namespace Terrarium.Services.Clients;

public sealed class ReportingServiceClient(HttpClient httpClient) : IReportingService
{
    public async Task<bool> ReportBugAsync(BugReport report, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("bugs/report", report, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SuccessResponse>(cancellationToken);
        return result?.Success ?? false;
    }

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

    public async Task<IReadOnlyList<SpeciesWithPopulation>> GetSpeciesListAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<SpeciesWithPopulation>>(
            "reporting/stats/species-list", cancellationToken);
        return result ?? [];
    }

    public async Task<IReadOnlyList<SpeciesPopulationData>> GetLatestSpeciesDataAsync(string species,
        CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<SpeciesPopulationData>>(
            $"reporting/stats/latest?species={Uri.EscapeDataString(species)}", cancellationToken);
        return result ?? [];
    }

    public async Task<IReadOnlyList<TopAnimalEntry>> GetTopAnimalsAsync(string version, string type, int? count,
        CancellationToken cancellationToken = default)
    {
        var url = $"reporting/stats/top-animals?version={Uri.EscapeDataString(version)}&type={Uri.EscapeDataString(type)}";
        if (count.HasValue)
            url += $"&count={count.Value}";
        var result = await httpClient.GetFromJsonAsync<List<TopAnimalEntry>>(url, cancellationToken);
        return result ?? [];
    }

    private sealed class SuccessResponse
    {
        public bool Success { get; init; }
    }
}