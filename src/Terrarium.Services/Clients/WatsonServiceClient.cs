using System.Net.Http.Json;
using Terrarium.Services.Interfaces;
using Terrarium.Services.Models;

namespace Terrarium.Services.Clients;

public sealed class WatsonServiceClient(HttpClient httpClient) : IWatsonService
{
    public async Task<bool> ReportErrorAsync(WatsonReport report, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("watson/report", new
        {
            report.LogType,
            report.OSVersion,
            report.GameVersion,
            clrVersion = report.CLRVersion,
            report.ErrorLog,
            report.UserEmail,
            report.UserComment
        }, cancellationToken);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SuccessResponse>(cancellationToken);
        return result?.Success ?? false;
    }

    private sealed class SuccessResponse
    {
        public bool Success { get; init; }
    }
}
