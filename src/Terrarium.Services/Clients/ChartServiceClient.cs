using System.Net.Http.Json;
using Terrarium.Services.Interfaces;
using Terrarium.Services.Models;

namespace Terrarium.Services.Clients;

public sealed class ChartServiceClient(HttpClient httpClient) : IChartService
{
    public async Task<IReadOnlyList<SpeciesInfo>> GetSpeciesListAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<SpeciesInfo>>("charts/species", cancellationToken);
        return result ?? [];
    }

    public async Task<string> ChartPopulationAsync(DateTime start, DateTime end, IReadOnlyList<string> speciesNames,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("charts/population",
            new { start, end, speciesNames }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken) ?? string.Empty;
    }

    public async Task<string> ChartVitalsAsync(DateTime start, DateTime end, string species,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(
            $"charts/vitals?start={start:O}&end={end:O}&species={Uri.EscapeDataString(species)}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken) ?? string.Empty;
    }

    public async Task<IReadOnlyList<SpeciesInfo>> GrabLatestSpeciesDataAsync(string species,
        CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<SpeciesInfo>>(
            $"charts/species/{Uri.EscapeDataString(species)}/latest", cancellationToken);
        return result ?? [];
    }

    public async Task<IReadOnlyList<SpeciesInfo>> GetTopAnimalsAsync(string version, OrganismType type, int count,
        CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<SpeciesInfo>>(
            $"charts/top?version={Uri.EscapeDataString(version)}&type={type}&count={count}", cancellationToken);
        return result ?? [];
    }
}
