using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Terrarium.Server.Tests;

/// <summary>
/// Integration tests for the Species endpoints (/api/species/*).
/// Endpoints that require a SQL connection gracefully return defaults when the DB is unavailable.
/// </summary>
public class SpeciesEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public SpeciesEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithEmptyBody_ReturnsVersionIncompatible()
    {
        var request = new RegisterSpeciesRequest();
        var response = await _client.PostAsJsonAsync("/api/species/register", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RegisterSpeciesResponse>();
        Assert.NotNull(result);
        Assert.Equal(SpeciesServiceStatus.VersionIncompatible, result!.Status);
    }

    [Fact]
    public async Task Register_WithMissingName_ReturnsVersionIncompatible()
    {
        var request = new RegisterSpeciesRequest
        {
            Version = "1.0.0.0",
            Type = "Animal",
            Author = "Test",
            Email = "test@test.com",
            AssemblyFullName = "TestAssembly",
            AssemblyCode = new byte[] { 1, 2, 3 }
        };
        var response = await _client.PostAsJsonAsync("/api/species/register", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RegisterSpeciesResponse>();
        Assert.Equal(SpeciesServiceStatus.VersionIncompatible, result!.Status);
    }

    [Fact]
    public async Task Register_WithMissingAssemblyCode_ReturnsVersionIncompatible()
    {
        var request = new RegisterSpeciesRequest
        {
            Name = "TestSpecies",
            Version = "1.0.0.0",
            Type = "Animal",
            Author = "Test",
            Email = "test@test.com",
            AssemblyFullName = "TestAssembly"
        };
        var response = await _client.PostAsJsonAsync("/api/species/register", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RegisterSpeciesResponse>();
        Assert.Equal(SpeciesServiceStatus.VersionIncompatible, result!.Status);
    }

    [Fact]
    public async Task Register_WithFullPayload_ReturnsServerDown_WhenNoDb()
    {
        var request = new RegisterSpeciesRequest
        {
            Name = "TestSpecies",
            Version = "1.0.0.0",
            Type = "Animal",
            Author = "TestAuthor",
            Email = "test@test.com",
            AssemblyFullName = "Test.Assembly, Version=1.0.0.0",
            AssemblyCode = new byte[] { 0x4D, 0x5A, 0x90, 0x00 }
        };
        var response = await _client.PostAsJsonAsync("/api/species/register", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<RegisterSpeciesResponse>();
        Assert.NotNull(result);
        Assert.Equal(SpeciesServiceStatus.ServerDown, result!.Status);
    }

    [Fact]
    public async Task List_WithoutVersion_ReturnsEmptyArray()
    {
        var response = await _client.GetAsync("/api/species/list?version=");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task List_WithVersion_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/species/list?version=1.0.0.0");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task List_WithFilterAll_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/species/list?version=1.0.0.0&filter=All");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Extinct_WithoutVersion_ReturnsEmptyArray()
    {
        var response = await _client.GetAsync("/api/species/extinct?version=");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
        Assert.Equal(0, doc.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task Extinct_WithVersion_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/species/extinct?version=1.0.0.0");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Blacklisted_ReturnsOk_WithArray()
    {
        var response = await _client.GetAsync("/api/species/blacklisted");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        Assert.Equal(JsonValueKind.Array, doc.RootElement.ValueKind);
    }

    [Fact]
    public async Task Reintroduce_WithEmptyRequest_ReturnsFalse()
    {
        var request = new ReintroduceSpeciesRequest();
        var response = await _client.PostAsJsonAsync("/api/species/reintroduce", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ReintroduceSpeciesResponse>();
        Assert.NotNull(result);
        Assert.False(result!.Success);
    }

    [Fact]
    public async Task Reintroduce_WithMissingName_ReturnsFalse()
    {
        var request = new ReintroduceSpeciesRequest
        {
            Version = "1.0.0.0",
            PeerGuid = Guid.NewGuid()
        };
        var response = await _client.PostAsJsonAsync("/api/species/reintroduce", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ReintroduceSpeciesResponse>();
        Assert.False(result!.Success);
    }

    [Fact]
    public async Task Reintroduce_WithMissingGuid_ReturnsFalse()
    {
        var request = new ReintroduceSpeciesRequest
        {
            Name = "TestSpecies",
            Version = "1.0.0.0"
        };
        var response = await _client.PostAsJsonAsync("/api/species/reintroduce", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ReintroduceSpeciesResponse>();
        Assert.False(result!.Success);
    }

    [Fact]
    public async Task Reintroduce_WithValidPayload_ReturnsFalse_WhenNoDb()
    {
        var request = new ReintroduceSpeciesRequest
        {
            Name = "TestSpecies",
            Version = "1.0.0.0",
            PeerGuid = Guid.NewGuid()
        };
        var response = await _client.PostAsJsonAsync("/api/species/reintroduce", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<ReintroduceSpeciesResponse>();
        Assert.NotNull(result);
        Assert.False(result!.Success);
    }
}
