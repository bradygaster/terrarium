using System.Net.Http.Json;
using Terrarium.Services.Interfaces;
using Terrarium.Services.Models;

namespace Terrarium.Services.Clients;

public sealed class PopulationServiceClient(HttpClient httpClient) : IPopulationService
{
    public async Task<int> ReportPopulationAsync(PopulationData data, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("population/report", data, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<int>(cancellationToken);
    }
}
