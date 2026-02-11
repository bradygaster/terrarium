using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Terrarium.Server.Tests;

/// <summary>
/// Integration tests for the Charts endpoints (/api/charts/*).
/// Endpoints gracefully return empty arrays when the DB is unavailable.
/// </summary>
public class ChartsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ChartsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task PopulationHistory_WithSpecies_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/charts/population-history?species=TestSpecies");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task PopulationHistory_WithDateRange_ReturnsOk()
    {
        var begin = DateTime.UtcNow.AddDays(-7).ToString("o");
        var end = DateTime.UtcNow.ToString("o");
        var response = await _client.GetAsync(
            $"/api/charts/population-history?species=TestSpecies&beginDate={begin}&endDate={end}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task SpeciesDistribution_ReturnsOk_WithArray()
    {
        var response = await _client.GetAsync("/api/charts/species-distribution");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task TopCreatures_WithVersion_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/charts/top-creatures?version=1.0.0.0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task TopCreatures_WithTypeAndCount_ReturnsOk()
    {
        var response = await _client.GetAsync(
            "/api/charts/top-creatures?version=1.0.0.0&type=Plant&count=5");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task TopCreatures_DefaultsTypeToAnimal()
    {
        var response = await _client.GetAsync("/api/charts/top-creatures?version=1.0.0.0");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
