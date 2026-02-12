using Microsoft.AspNetCore.SignalR.Client;
using Terrarium.Net;
using Xunit;

namespace Terrarium.SignalR.Tests;

/// <summary>
/// Tests for error handling via ReceiveError callback.
/// Per architecture doc (Section 9): hub never throws — returns errors via ReceiveError.
/// </summary>
public class ErrorHandlingTests : IClassFixture<TerrariumHubFactory>, IAsyncLifetime
{
    private readonly TerrariumHubFactory _factory;
    private HubConnection _connection = null!;

    public ErrorHandlingTests(TerrariumHubFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync()
    {
        _connection = _factory.CreateHubConnection();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_connection.State != HubConnectionState.Disconnected)
            await _connection.StopAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task Hub_Never_Throws_On_JoinEcosystem()
    {
        await _connection.StartAsync();

        var exception = await Record.ExceptionAsync(() =>
            _connection.InvokeAsync("JoinEcosystem", "test-eco-1"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Hub_Never_Throws_On_LeaveEcosystem()
    {
        await _connection.StartAsync();

        var exception = await Record.ExceptionAsync(() =>
            _connection.InvokeAsync("LeaveEcosystem", "test-eco-1"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Hub_Never_Throws_On_Heartbeat()
    {
        await _connection.StartAsync();

        var exception = await Record.ExceptionAsync(() =>
            _connection.InvokeAsync("Heartbeat", "test-eco-1"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Hub_Never_Throws_On_TeleportCreature()
    {
        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");

        var exception = await Record.ExceptionAsync(() =>
            _connection.InvokeAsync("TeleportCreature", new CreatureTeleport
            {
                TeleportId = Guid.NewGuid().ToString(),
                EcosystemId = "test-eco-1",
                OrganismId = "org-1",
                SpeciesAssemblyName = "Test, Version=1.0.0.0",
                SourcePeerId = _connection.ConnectionId!,
                StatePayload = "{}"
            }));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Hub_Never_Throws_On_ReportPopulation()
    {
        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");

        var exception = await Record.ExceptionAsync(() =>
            _connection.InvokeAsync("ReportPopulation", new PopulationReport
            {
                EcosystemId = "test-eco-1",
                TickNumber = 1,
                Species = [],
                TotalOrganisms = 0
            }));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Hub_Never_Throws_On_RequestWorldState()
    {
        await _connection.StartAsync();

        var exception = await Record.ExceptionAsync(async () =>
        {
            var result = await _connection.InvokeAsync<WorldStateUpdate>("RequestWorldState", "test-eco-1");
            Assert.NotNull(result);
        });

        Assert.Null(exception);
    }

    [Fact]
    public async Task Hub_Never_Throws_On_RequestPeerList()
    {
        await _connection.StartAsync();

        var exception = await Record.ExceptionAsync(async () =>
        {
            var result = await _connection.InvokeAsync<PeerListResponse>("RequestPeerList", "test-eco-1");
            Assert.NotNull(result);
        });

        Assert.Null(exception);
    }

    // TODO: Once rate limiting is implemented on the hub (Sprint 7), add these tests:
    //
    // [Fact] Hub_Returns_ReceiveError_With_RATE_LIMITED_Code_When_Throttled
    // [Fact] Hub_Returns_ReceiveError_With_UNKNOWN_SPECIES_On_Invalid_Teleport
    // [Fact] Hub_Returns_ReceiveError_With_IsTransient_True_For_RATE_LIMITED
    // [Fact] Hub_Returns_ReceiveError_With_RetryAfterMs_For_RATE_LIMITED
    // [Fact] Hub_Returns_ReceiveError_With_VERSION_MISMATCH_On_Old_Client
    // [Fact] Hub_Returns_ReceiveError_With_ECOSYSTEM_FULL_At_Max_Capacity
}
