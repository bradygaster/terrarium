using Microsoft.AspNetCore.SignalR.Client;
using Terrarium.Net;
using Xunit;

namespace Terrarium.SignalR.Tests;

/// <summary>
/// Tests for hub connection lifecycle: connect, join ecosystem, heartbeat, leave, disconnect.
/// Validates the state machine from Heisenberg's architecture doc (Section 7).
/// </summary>
public class ConnectionLifecycleTests : IClassFixture<TerrariumHubFactory>, IAsyncLifetime
{
    private readonly TerrariumHubFactory _factory;
    private HubConnection _connection = null!;

    public ConnectionLifecycleTests(TerrariumHubFactory factory)
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
        {
            await _connection.StopAsync();
        }
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task Client_Can_Connect_To_Hub()
    {
        await _connection.StartAsync();

        Assert.Equal(HubConnectionState.Connected, _connection.State);
    }

    [Fact]
    public async Task Client_Can_Join_Ecosystem()
    {
        await _connection.StartAsync();

        // JoinEcosystem should complete without error
        await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");

        // If we get here without exception, the hub accepted the call
        Assert.Equal(HubConnectionState.Connected, _connection.State);
    }

    [Fact]
    public async Task Client_Can_Leave_Ecosystem()
    {
        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");

        await _connection.InvokeAsync("LeaveEcosystem", "test-eco-1");

        Assert.Equal(HubConnectionState.Connected, _connection.State);
    }

    [Fact]
    public async Task Client_Can_Send_Heartbeat()
    {
        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");

        // Heartbeat should complete without error
        await _connection.InvokeAsync("Heartbeat", "test-eco-1");

        Assert.Equal(HubConnectionState.Connected, _connection.State);
    }

    [Fact]
    public async Task Client_Can_Disconnect_Cleanly()
    {
        await _connection.StartAsync();
        Assert.Equal(HubConnectionState.Connected, _connection.State);

        await _connection.StopAsync();

        Assert.Equal(HubConnectionState.Disconnected, _connection.State);
    }

    [Fact]
    public async Task Client_Can_Join_Then_Leave_Then_Rejoin()
    {
        await _connection.StartAsync();

        await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");
        await _connection.InvokeAsync("LeaveEcosystem", "test-eco-1");
        await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");

        Assert.Equal(HubConnectionState.Connected, _connection.State);
    }

    [Fact]
    public async Task Multiple_Heartbeats_Succeed()
    {
        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");

        // Per architecture doc: heartbeat every 30 seconds, 3 per 60s window
        for (int i = 0; i < 3; i++)
        {
            await _connection.InvokeAsync("Heartbeat", "test-eco-1");
        }

        Assert.Equal(HubConnectionState.Connected, _connection.State);
    }

    [Fact]
    public async Task Client_Can_Join_Multiple_Ecosystems()
    {
        await _connection.StartAsync();

        await _connection.InvokeAsync("JoinEcosystem", "eco-1");
        await _connection.InvokeAsync("JoinEcosystem", "eco-2");

        Assert.Equal(HubConnectionState.Connected, _connection.State);
    }
}
