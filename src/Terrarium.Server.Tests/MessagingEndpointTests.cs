using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Text.Json;
using Xunit;

namespace Terrarium.Server.Tests;

/// <summary>
/// Tests for the messaging endpoints.
/// 
/// Legacy behavior (Server/Website/App_Code/Messaging/Messaging.asmx.cs):
///   - GetWelcomeMessage() → ServerSettings.WelcomeMessage, default "Welcome to .NET Terrarium 2.0!"
///   - GetMessageOfTheDay() → ServerSettings.MOTD, default "Have Fun!"
///   - GetLatestVersion() → ServerSettings.LatestVersion, default "1.0.0.0"
///
/// Expected modern endpoints:
///   GET /api/messaging/welcome → JSON with welcome message
///   GET /api/messaging/motd    → JSON with message of the day
///   GET /api/messaging/version → JSON with server version info
///
/// These tests will pass once the server messaging endpoints are implemented.
/// </summary>
public class MessagingEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public MessagingEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Welcome_Returns_OK()
    {
        var response = await _client.GetAsync("/api/messaging/welcome");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Welcome_Returns_Json_With_Message()
    {
        var response = await _client.GetAsync("/api/messaging/welcome");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("message", out var prop),
            "Response should have a 'message' property");
        Assert.False(string.IsNullOrWhiteSpace(prop.GetString()),
            "Welcome message should not be empty");
    }

    [Fact]
    public async Task Welcome_Default_Contains_Terrarium()
    {
        // Default from legacy ServerSettings is "Welcome to .NET Terrarium 2.0!"
        var response = await _client.GetAsync("/api/messaging/welcome");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var message = doc.RootElement.GetProperty("message").GetString()!;

        Assert.Contains("Terrarium", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Motd_Returns_OK()
    {
        var response = await _client.GetAsync("/api/messaging/motd");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Motd_Returns_Json_With_Message()
    {
        var response = await _client.GetAsync("/api/messaging/motd");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("message", out var prop),
            "Response should have a 'message' property");
        Assert.False(string.IsNullOrWhiteSpace(prop.GetString()),
            "Message of the day should not be empty");
    }

    [Fact]
    public async Task Version_Returns_OK()
    {
        var response = await _client.GetAsync("/api/messaging/version");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Version_Returns_Json_With_Version()
    {
        var response = await _client.GetAsync("/api/messaging/version");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);

        Assert.True(doc.RootElement.TryGetProperty("version", out var prop),
            "Response should have a 'version' property");
        Assert.False(string.IsNullOrWhiteSpace(prop.GetString()),
            "Version info should not be empty");
    }

    [Fact]
    public async Task Version_Default_Looks_Like_A_Version()
    {
        // Default from legacy ServerSettings is "1.0.0.0"
        var response = await _client.GetAsync("/api/messaging/version");
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var version = doc.RootElement.GetProperty("version").GetString()!;

        Assert.Matches(@"\d+\.\d+", version);
    }
}
