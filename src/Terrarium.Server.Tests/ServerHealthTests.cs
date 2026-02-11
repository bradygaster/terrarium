using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace Terrarium.Server.Tests;

/// <summary>
/// Tests that the server starts and responds to basic requests.
/// Based on legacy Server/Website behavior — the server must boot and serve traffic.
/// </summary>
public class ServerHealthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ServerHealthTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Server_Starts_And_Responds()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.True(response.IsSuccessStatusCode,
            $"Root endpoint returned {response.StatusCode}");
    }

    [Fact]
    public async Task Root_Returns_Terrarium_Server()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal("Terrarium Server", content);
    }

    [Fact]
    public async Task Health_Endpoint_Returns_Healthy()
    {
        // ServiceDefaults maps /health via MapDefaultEndpoints()
        // This will pass once Gus's server bootstrap lands with AddServiceDefaults()
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Alive_Endpoint_Returns_OK()
    {
        // ServiceDefaults maps /alive via MapDefaultEndpoints()
        // This will pass once Gus's server bootstrap lands with AddServiceDefaults()
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/alive");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
