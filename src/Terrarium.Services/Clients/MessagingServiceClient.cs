using System.Net.Http.Json;
using Terrarium.Services.Interfaces;

namespace Terrarium.Services.Clients;

public sealed class MessagingServiceClient(HttpClient httpClient) : IMessagingService
{
    public async Task<string> GetWelcomeMessageAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<MessageResponse>("messaging/welcome", cancellationToken);
        return result?.Message ?? string.Empty;
    }

    public async Task<string> GetMessageOfTheDayAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<MessageResponse>("messaging/motd", cancellationToken);
        return result?.Message ?? string.Empty;
    }

    public async Task<string> GetLatestVersionAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<VersionResponse>("messaging/version", cancellationToken);
        return result?.Version ?? string.Empty;
    }

    private sealed class MessageResponse
    {
        public string? Message { get; init; }
    }

    private sealed class VersionResponse
    {
        public string? Version { get; init; }
    }
}
