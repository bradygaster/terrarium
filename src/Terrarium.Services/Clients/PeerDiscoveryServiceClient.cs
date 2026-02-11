using System.Net.Http.Json;
using Terrarium.Services.Interfaces;
using Terrarium.Services.Models;

namespace Terrarium.Services.Clients;

public sealed class PeerDiscoveryServiceClient(HttpClient httpClient) : IPeerDiscoveryService
{
    public async Task<bool> RegisterUserAsync(string email, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("discovery/register-user", new { email }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<bool>(cancellationToken);
    }

    public async Task<int> GetNumPeersAsync(string version, string channel, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"discovery/peers/count?version={Uri.EscapeDataString(version)}&channel={Uri.EscapeDataString(channel)}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<int>(cancellationToken);
    }

    public async Task<string> ValidatePeerAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync("discovery/validate", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken) ?? string.Empty;
    }

    public async Task<PeerRegistrationResult> RegisterPeerAsync(string version, string channel, Guid peerGuid, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("discovery/register", new { version, channel, peerGuid }, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PeerRegistrationResult>(cancellationToken)
            ?? new PeerRegistrationResult { Status = RegisterPeerResult.Failure };
    }

    public async Task<VersionCheckResult> IsVersionDisabledAsync(string version, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync($"discovery/version-check?version={Uri.EscapeDataString(version)}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VersionCheckResult>(cancellationToken)
            ?? new VersionCheckResult { IsDisabled = true, ErrorMessage = "Failed to check version status" };
    }
}
