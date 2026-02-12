using Microsoft.AspNetCore.SignalR.Client;
using Terrarium.Net;
using Xunit;

namespace Terrarium.SignalR.Tests;

/// <summary>
/// Edge case tests that don't fit neatly into other categories.
/// These are the scenarios that break in production but nobody writes tests for.
/// </summary>
public class EdgeCaseTests : IClassFixture<TerrariumHubFactory>, IAsyncLifetime
{
    private readonly TerrariumHubFactory _factory;
    private HubConnection _connection = null!;

    public EdgeCaseTests(TerrariumHubFactory factory)
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
    public async Task Heartbeat_Before_Join_Does_Not_Crash()
    {
        await _connection.StartAsync();

        // Heartbeat without joining any ecosystem — hub should handle gracefully
        var exception = await Record.ExceptionAsync(() =>
            _connection.InvokeAsync("Heartbeat", "nonexistent-eco"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Heartbeat_After_Leave_Does_Not_Crash()
    {
        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");
        await _connection.InvokeAsync("LeaveEcosystem", "test-eco-1");

        // Heartbeat after leaving — should not throw
        var exception = await Record.ExceptionAsync(() =>
            _connection.InvokeAsync("Heartbeat", "test-eco-1"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Leave_Without_Join_Does_Not_Crash()
    {
        await _connection.StartAsync();

        // Leaving an ecosystem we never joined — should not throw
        var exception = await Record.ExceptionAsync(() =>
            _connection.InvokeAsync("LeaveEcosystem", "never-joined-eco"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task RequestWorldState_Returns_Valid_Structure()
    {
        await _connection.StartAsync();

        var worldState = await _connection.InvokeAsync<WorldStateUpdate>("RequestWorldState", "test-eco-1");

        Assert.NotNull(worldState);
        Assert.Equal("test-eco-1", worldState.EcosystemId);
        Assert.True(worldState.TickNumber >= 0);
        Assert.True(worldState.WorldWidth >= 0);
        Assert.True(worldState.WorldHeight >= 0);
        Assert.True(worldState.OrganismCount >= 0);
    }

    [Fact]
    public async Task RequestPeerList_Returns_Valid_Structure()
    {
        await _connection.StartAsync();

        var peerList = await _connection.InvokeAsync<PeerListResponse>("RequestPeerList", "test-eco-1");

        Assert.NotNull(peerList);
        Assert.Equal("test-eco-1", peerList.EcosystemId);
        Assert.NotNull(peerList.Peers);
    }

    [Fact]
    public async Task Multiple_Rapid_Joins_And_Leaves_Do_Not_Crash()
    {
        await _connection.StartAsync();

        for (int i = 0; i < 10; i++)
        {
            await _connection.InvokeAsync("JoinEcosystem", $"rapid-eco-{i}");
            await _connection.InvokeAsync("LeaveEcosystem", $"rapid-eco-{i}");
        }

        Assert.Equal(HubConnectionState.Connected, _connection.State);
    }

    [Fact]
    public async Task Teleport_With_Empty_StatePayload_Does_Not_Crash()
    {
        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");

        var exception = await Record.ExceptionAsync(() =>
            _connection.InvokeAsync("TeleportCreature", new CreatureTeleport
            {
                TeleportId = Guid.NewGuid().ToString(),
                EcosystemId = "test-eco-1",
                OrganismId = "empty-state-org",
                SpeciesAssemblyName = "Test, Version=1.0.0.0",
                SourcePeerId = _connection.ConnectionId!,
                StatePayload = ""
            }));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Population_Report_With_Zero_Species_Does_Not_Crash()
    {
        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");

        var exception = await Record.ExceptionAsync(() =>
            _connection.InvokeAsync("ReportPopulation", new PopulationReport
            {
                EcosystemId = "test-eco-1",
                TickNumber = 0,
                Species = [],
                TotalOrganisms = 0
            }));

        Assert.Null(exception);
    }

    [Fact]
    public async Task AnnouncePeer_With_No_Version_Does_Not_Crash()
    {
        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");

        var exception = await Record.ExceptionAsync(() =>
            _connection.InvokeAsync("AnnouncePeer", new PeerAnnounce
            {
                PeerId = _connection.ConnectionId!,
                EcosystemId = "test-eco-1",
                Action = PeerAction.Join,
                Version = null,
                Channel = null
            }));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Concurrent_Hub_Operations_Do_Not_Deadlock()
    {
        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");

        // Fire multiple operations concurrently
        var tasks = new[]
        {
            _connection.InvokeAsync("Heartbeat", "test-eco-1"),
            _connection.InvokeAsync<WorldStateUpdate>("RequestWorldState", "test-eco-1"),
            _connection.InvokeAsync<PeerListResponse>("RequestPeerList", "test-eco-1"),
            _connection.InvokeAsync("ReportPopulation", new PopulationReport
            {
                EcosystemId = "test-eco-1",
                TickNumber = 1,
                Species = [],
                TotalOrganisms = 0
            })
        };

        var allTasks = Task.WhenAll(tasks);

        // Should complete within a reasonable time — no deadlock
        var completed = await Task.WhenAny(allTasks, Task.Delay(TimeSpan.FromSeconds(10)));

        Assert.True(allTasks.IsCompletedSuccessfully, "Concurrent hub operations should complete without deadlock");
    }
}
