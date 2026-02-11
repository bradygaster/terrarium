using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Terrarium.Server.Tests;

/// <summary>
/// Integration tests for the Peer Discovery endpoints (/api/discovery/*).
/// Tests peer registration, listing, validation, user registration, and version check.
/// </summary>
public class PeerDiscoveryEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public PeerDiscoveryEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithEmptyVersion_ReturnsGlobalFailure()
    {
        var request = new RegisterPeerRequest
        {
            Version = "",
            Channel = "test",
            Guid = Guid.NewGuid()
        };
        var response = await _client.PostAsJsonAsync("/api/discovery/register", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RegisterPeerResponse>();
        Assert.NotNull(result);
        Assert.Equal(RegisterPeerResult.GlobalFailure, result!.Result);
    }

    [Fact]
    public async Task Register_WithEmptyChannel_ReturnsGlobalFailure()
    {
        var request = new RegisterPeerRequest
        {
            Version = "1.0.0.0",
            Channel = "",
            Guid = Guid.NewGuid()
        };
        var response = await _client.PostAsJsonAsync("/api/discovery/register", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RegisterPeerResponse>();
        Assert.Equal(RegisterPeerResult.GlobalFailure, result!.Result);
    }

    [Fact]
    public async Task Register_WithValidPayload_ReturnsFailure_WhenNoDb()
    {
        var request = new RegisterPeerRequest
        {
            Version = "1.0.0.0",
            Channel = "test-channel",
            Guid = Guid.NewGuid()
        };
        var response = await _client.PostAsJsonAsync("/api/discovery/register", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RegisterPeerResponse>();
        Assert.NotNull(result);
        Assert.Equal(RegisterPeerResult.Failure, result!.Result);
    }

    [Fact]
    public async Task Peers_WithEmptyVersion_ReturnsZeroCount()
    {
        var response = await _client.GetAsync("/api/discovery/peers?version=&channel=test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PeerCountResponse>();
        Assert.NotNull(result);
        Assert.Equal(0, result!.Count);
    }

    [Fact]
    public async Task Peers_WithEmptyChannel_ReturnsZeroCount()
    {
        var response = await _client.GetAsync("/api/discovery/peers?version=1.0.0.0&channel=");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PeerCountResponse>();
        Assert.Equal(0, result!.Count);
    }

    [Fact]
    public async Task Peers_WithValidParams_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/discovery/peers?version=1.0.0.0&channel=test");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PeerCountResponse>();
        Assert.NotNull(result);
        Assert.Equal(0, result!.Count);
    }

    [Fact]
    public async Task Validate_ReturnsOk_WithIpAddress()
    {
        var response = await _client.GetAsync("/api/discovery/validate");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("ipAddress", out _));
    }

    [Fact]
    public async Task RegisterUser_ReturnsOk()
    {
        var request = new RegisterUserRequest { Email = "test@example.com" };
        var response = await _client.PostAsJsonAsync("/api/discovery/register-user", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.True(doc.RootElement.TryGetProperty("success", out _));
    }

    [Fact]
    public async Task VersionCheck_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/discovery/version-check?version=1.0.0.0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task VersionCheck_ReturnsDisabledTrue_WhenNoDb()
    {
        var response = await _client.GetAsync("/api/discovery/version-check?version=1.0.0.0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<VersionCheckResponse>();
        Assert.NotNull(result);
        Assert.True(result!.Disabled);
    }
}
