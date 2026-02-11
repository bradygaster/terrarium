using System.Net.Http.Json;
using Terrarium.Services.Interfaces;
using Terrarium.Services.Models;

namespace Terrarium.Services.Clients;

public sealed class UsageServiceClient(HttpClient httpClient) : IUsageService
{
    public async Task<HeartbeatResult> SendHeartbeatAsync(Guid peerGuid, int currentTick,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("usage/heartbeat",
            new { guid = peerGuid, currentTick }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<HeartbeatResult>(cancellationToken)
            ?? new HeartbeatResult { Success = false };
    }

    public async Task<DailyStats> GetDailyStatsAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<DailyStats>("usage/daily-stats", cancellationToken);
        return result ?? new DailyStats();
    }

    public async Task<ServerVersionInfo> GetServerVersionAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<ServerVersionInfo>("usage/version-check", cancellationToken);
        return result ?? new ServerVersionInfo();
    }
}
