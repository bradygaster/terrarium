using System.Net.Http.Json;
using Terrarium.Services.Interfaces;
using Terrarium.Services.Models;

namespace Terrarium.Services.Clients;

public sealed class PeerDiscoveryServiceClient(HttpClient httpClient) : IPeerDiscoveryService
{
    public async Task<bool> RegisterUserAsync(string email, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("discovery/register-user",
            new { email }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<SuccessResponse>(cancellationToken);
        return result?.Success ?? false;
    }

    public async Task<int> GetNumPeersAsync(string version, string channel, CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<PeerCountResponse>(
            $"discovery/peers?version={Uri.EscapeDataString(version)}&channel={Uri.EscapeDataString(channel)}", cancellationToken);
        return result?.Count ?? 0;
    }

    public async Task<string> ValidatePeerAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<ValidateResponse>(
            "discovery/validate", cancellationToken);
        return result?.IpAddress ?? string.Empty;
    }

    public async Task<PeerRegistrationResult> RegisterPeerAsync(string version, string channel, Guid peerGuid, CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("discovery/register",
            new { version, channel, guid = peerGuid }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RegisterPeerResponse>(cancellationToken);
        if (result is null)
            return new PeerRegistrationResult { Status = RegisterPeerResult.Failure };

        return new PeerRegistrationResult
        {
            Status = result.Result,
            PeerCount = result.PeerCount,
            Peers = result.Peers?.Select(p => new PeerInfo
            {
                IPAddress = p.IPAddress,
                Version = version,
                LastContact = p.Lease
            }).ToList() ?? []
        };
    }

    public async Task<VersionCheckResult> IsVersionDisabledAsync(string version, CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<VersionCheckResponse>(
            $"discovery/version-check?version={Uri.EscapeDataString(version)}", cancellationToken);
        return new VersionCheckResult
        {
            IsDisabled = result?.Disabled ?? true,
            ErrorMessage = result?.Message
        };
    }

    private sealed class SuccessResponse
    {
        public bool Success { get; init; }
    }

    private sealed class PeerCountResponse
    {
        public int Count { get; init; }
    }

    private sealed class ValidateResponse
    {
        public string? IpAddress { get; init; }
    }

    private sealed class RegisterPeerResponse
    {
        public RegisterPeerResult Result { get; init; }
        public int PeerCount { get; init; }
        public List<PeerEntry>? Peers { get; init; }
    }

    private sealed class PeerEntry
    {
        public string IPAddress { get; init; } = string.Empty;
        public DateTime Lease { get; init; }
    }

    private sealed class VersionCheckResponse
    {
        public bool Disabled { get; init; }
        public string? Message { get; init; }
    }
}
