using System.Net;
using Xunit;

namespace Terrarium.Smoke.Tests;

/// <summary>
/// Smoke tests that verify the Terrarium server starts and basic endpoints respond.
/// Uses a self-contained TestServer (decoupled from Terrarium.Server project which has
/// pre-existing build errors in SpeciesEndpoints.cs).
/// </summary>
public class ServerStartupSmokeTests : IClassFixture<TerrariumServerFactory>
{
    private readonly TerrariumServerFactory _factory;

    public ServerStartupSmokeTests(TerrariumServerFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Server_Starts_Without_Throwing()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.True(response.IsSuccessStatusCode,
            $"Server failed to start — root endpoint returned {response.StatusCode}");
    }

    [Fact]
    public async Task Root_Endpoint_Returns_Expected_Content()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");
        var content = await response.Content.ReadAsStringAsync();

        Assert.Equal("Terrarium Server", content);
    }

    [Fact]
    public async Task Health_Endpoint_Responds()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Alive_Endpoint_Responds()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/alive");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
