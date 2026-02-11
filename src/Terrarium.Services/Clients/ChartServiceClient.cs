using System.Net.Http.Json;
using Terrarium.Services.Interfaces;
using Terrarium.Services.Models;

namespace Terrarium.Services.Clients;

public sealed class ChartServiceClient(HttpClient httpClient) : IChartService
{
    public async Task<IReadOnlyList<PopulationHistoryEntry>> GetPopulationHistoryAsync(string species,
        DateTime? beginDate = null, DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"charts/population-history?species={Uri.EscapeDataString(species)}";
        if (beginDate.HasValue)
            url += $"&beginDate={beginDate.Value:O}";
        if (endDate.HasValue)
            url += $"&endDate={endDate.Value:O}";

        var result = await httpClient.GetFromJsonAsync<List<PopulationHistoryEntry>>(url, cancellationToken);
        return result ?? [];
    }

    public async Task<IReadOnlyList<SpeciesDistributionEntry>> GetSpeciesDistributionAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<SpeciesDistributionEntry>>(
            "charts/species-distribution", cancellationToken);
        return result ?? [];
    }

    public async Task<IReadOnlyList<TopCreatureEntry>> GetTopCreaturesAsync(string version,
        string? type = null, int? count = null,
        CancellationToken cancellationToken = default)
    {
        var url = $"charts/top-creatures?version={Uri.EscapeDataString(version)}";
        if (type is not null)
            url += $"&type={Uri.EscapeDataString(type)}";
        if (count.HasValue)
            url += $"&count={count.Value}";

        var result = await httpClient.GetFromJsonAsync<List<TopCreatureEntry>>(url, cancellationToken);
        return result ?? [];
    }
}