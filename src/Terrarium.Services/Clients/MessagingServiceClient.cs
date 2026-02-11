using System.Net.Http.Json;
using Terrarium.Services.Interfaces;

namespace Terrarium.Services.Clients;

public sealed class MessagingServiceClient(HttpClient httpClient) : IMessagingService
{
    public async Task<string> GetWelcomeMessageAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("messaging/welcome", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken) ?? string.Empty;
    }

    public async Task<string> GetMessageOfTheDayAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("messaging/motd", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken) ?? string.Empty;
    }

    public async Task<string> GetLatestVersionAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("messaging/version", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken) ?? string.Empty;
    }
}
