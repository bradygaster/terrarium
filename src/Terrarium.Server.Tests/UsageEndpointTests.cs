using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Terrarium.Server.Tests;

/// <summary>
/// Integration tests for the Usage endpoints (/api/usage/*).
/// Tests heartbeat validation, daily stats, and version check.
/// </summary>
public class UsageEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UsageEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Heartbeat_WithEmptyGuid_ReturnsFalse()
    {
        var request = new HeartbeatRequest { Guid = Guid.Empty, CurrentTick = 1 };
        var response = await _client.PostAsJsonAsync("/api/usage/heartbeat", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<HeartbeatResponse>();
        Assert.NotNull(result);
        Assert.False(result!.Success);
    }

    [Fact]
    public async Task Heartbeat_WithValidGuid_ReturnsOk_WhenNoDb()
    {
        var request = new HeartbeatRequest { Guid = Guid.NewGuid(), CurrentTick = 1 };
        var response = await _client.PostAsJsonAsync("/api/usage/heartbeat", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<HeartbeatResponse>();
        Assert.NotNull(result);
        Assert.False(result!.Success);
    }

    [Fact]
    public async Task DailyStats_ReturnsOk_WithDefaultValues()
    {
        var response = await _client.GetAsync("/api/usage/daily-stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<DailyStatsResponse>();
        Assert.NotNull(result);
        Assert.True(result!.ActivePeers >= 0);
        Assert.True(result.SpeciesCount >= 0);
        Assert.True(result.TotalPopulation >= 0);
    }

    [Fact]
    public async Task VersionCheck_ReturnsOk_WithSettings()
    {
        var response = await _client.GetAsync("/api/usage/version-check");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ServerVersionResponse>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrEmpty(result!.LatestVersion));
        Assert.False(string.IsNullOrEmpty(result.MOTD));
    }

    [Fact]
    public async Task VersionCheck_ReturnsDefaultVersion()
    {
        var response = await _client.GetAsync("/api/usage/version-check");
        var result = await response.Content.ReadFromJsonAsync<ServerVersionResponse>();

        Assert.Equal("1.0.0.0", result!.LatestVersion);
    }

    [Fact]
    public async Task VersionCheck_ReturnsDefaultMotd()
    {
        var response = await _client.GetAsync("/api/usage/version-check");
        var result = await response.Content.ReadFromJsonAsync<ServerVersionResponse>();

        Assert.Equal("Have Fun!", result!.MOTD);
    }
}
