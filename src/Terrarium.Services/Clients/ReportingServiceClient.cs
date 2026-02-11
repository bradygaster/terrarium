using System.Net.Http.Json;
using Terrarium.Services.Interfaces;
using Terrarium.Services.Models;

namespace Terrarium.Services.Clients;

public sealed class ReportingServiceClient(HttpClient httpClient) : IReportingService
{
    public async Task ReportBugAsync(BugReport report, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("reporting/bugs", report, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task ReportErrorAsync(string errorData, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("reporting/errors", new { errorData }, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task ReportUsageAsync(UsageData data, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("reporting/usage", data, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
