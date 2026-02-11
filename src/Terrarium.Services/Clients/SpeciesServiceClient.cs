using System.Net.Http.Json;
using Terrarium.Services.Interfaces;
using Terrarium.Services.Models;

namespace Terrarium.Services.Clients;

public sealed class SpeciesServiceClient(HttpClient httpClient) : ISpeciesService
{
    public async Task<SpeciesServiceStatus> AddAsync(string name, string version, string type, string author,
        string email, string assemblyFullName, byte[] assemblyCode,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("species/register", new
        {
            name,
            version,
            type,
            author,
            email,
            assemblyFullName,
            assemblyCode
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<RegisterSpeciesResponse>(cancellationToken);
        return result?.Status ?? SpeciesServiceStatus.ServerDown;
    }

    public async Task<IReadOnlyList<SpeciesInfo>> GetExtinctSpeciesAsync(string version, string filter,
        CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<SpeciesInfo>>(
            $"species/extinct?version={Uri.EscapeDataString(version)}&filter={Uri.EscapeDataString(filter)}", cancellationToken);
        return result ?? [];
    }

    public async Task<IReadOnlyList<SpeciesInfo>> GetAllSpeciesAsync(string version, string filter,
        CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<SpeciesInfo>>(
            $"species/list?version={Uri.EscapeDataString(version)}&filter={Uri.EscapeDataString(filter)}", cancellationToken);
        return result ?? [];
    }

    public async Task<byte[]> GetSpeciesAssemblyAsync(string name, string version,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetAsync(
            $"species/{Uri.EscapeDataString(name)}/assembly?version={Uri.EscapeDataString(version)}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    public async Task<byte[]> ReintroduceSpeciesAsync(string name, string version, Guid peerGuid,
        CancellationToken cancellationToken = default)
    {
        var response = await httpClient.PostAsJsonAsync("species/reintroduce", new
        {
            name,
            version,
            peerGuid
        }, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ReintroduceResponse>(cancellationToken);
        return result?.AssemblyCode ?? [];
    }

    public async Task<IReadOnlyList<string>> GetBlacklistedSpeciesAsync(CancellationToken cancellationToken = default)
    {
        var result = await httpClient.GetFromJsonAsync<List<string>>("species/blacklisted", cancellationToken);
        return result ?? [];
    }

    private sealed class RegisterSpeciesResponse
    {
        public SpeciesServiceStatus Status { get; init; }
    }

    private sealed class ReintroduceResponse
    {
        public bool Success { get; init; }
        public byte[]? AssemblyCode { get; init; }
    }
}