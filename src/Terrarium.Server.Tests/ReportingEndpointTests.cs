using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Terrarium.Server.Tests;

/// <summary>
/// Integration tests for the Reporting endpoints (/api/reporting/*).
/// Endpoints gracefully return defaults when the DB is unavailable.
/// </summary>
public class ReportingEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ReportingEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Population_WithEmptyGuid_ReturnsSuccess()
    {
        var request = new ReportPopulationRequest
        {
            Guid = Guid.Empty,
            CurrentTick = 0,
            History = []
        };
        var response = await _client.PostAsJsonAsync("/api/reporting/population", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ReportPopulationResponse>();
        Assert.NotNull(result);
        Assert.Equal(ReportingReturnCode.Success, result!.ReturnCode);
    }

    [Fact]
    public async Task Population_WithEmptyHistory_ReturnsSuccess()
    {
        var request = new ReportPopulationRequest
        {
            Guid = Guid.NewGuid(),
            CurrentTick = 1,
            History = []
        };
        var response = await _client.PostAsJsonAsync("/api/reporting/population", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ReportPopulationResponse>();
        Assert.Equal(ReportingReturnCode.Success, result!.ReturnCode);
    }

    [Fact]
    public async Task Population_WithHistory_ReturnsOk_WhenNoDb()
    {
        var request = new ReportPopulationRequest
        {
            Guid = Guid.NewGuid(),
            CurrentTick = 10,
            History =
            [
                new PopulationHistoryRow
                {
                    Guid = Guid.NewGuid(),
                    TickNumber = 10,
                    SpeciesName = "TestSpecies",
                    ClientTime = DateTime.UtcNow,
                    CorrectTime = 1,
                    Population = 50
                }
            ]
        };
        var response = await _client.PostAsJsonAsync("/api/reporting/population", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ReportPopulationResponse>();
        Assert.NotNull(result);
        Assert.Equal(ReportingReturnCode.Success, result!.ReturnCode);
    }

    [Fact]
    public async Task StatsSpeciesList_ReturnsOk_WithArray()
    {
        var response = await _client.GetAsync("/api/reporting/stats/species-list");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task StatsLatest_ReturnsOk_WithArray()
    {
        var response = await _client.GetAsync("/api/reporting/stats/latest?species=TestSpecies");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task StatsTopAnimals_ReturnsOk_WithArray()
    {
        var response = await _client.GetAsync("/api/reporting/stats/top-animals?version=1.0.0.0&type=Animal");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task StatsTopAnimals_WithCount_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/reporting/stats/top-animals?version=1.0.0.0&type=Animal&count=5");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
