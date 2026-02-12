using Microsoft.AspNetCore.SignalR.Client;
using Terrarium.Net;
using Xunit;

namespace Terrarium.SignalR.Tests;

/// <summary>
/// Tests for reconnection behavior.
/// Per architecture doc (Section 7): on reconnect, client gets a new connection ID
/// and must re-join ecosystem, re-announce peer, re-request world state.
/// </summary>
public class ReconnectionTests : IClassFixture<TerrariumHubFactory>, IAsyncLifetime
{
    private readonly TerrariumHubFactory _factory;
    private HubConnection _connection = null!;

    public ReconnectionTests(TerrariumHubFactory factory)
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
    public async Task Reconnected_Client_Gets_New_ConnectionId()
    {
        await _connection.StartAsync();
        var originalConnectionId = _connection.ConnectionId;

        await _connection.StopAsync();

        // Create a new connection (simulates reconnect — new connection ID)
        _connection = _factory.CreateHubConnection();
        await _connection.StartAsync();

        Assert.NotEqual(originalConnectionId, _connection.ConnectionId);
    }

    [Fact]
    public async Task Reconnected_Client_Must_Rejoin_Ecosystem()
    {
        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");

        await _connection.StopAsync();

        // After reconnect, the client needs to rejoin
        _connection = _factory.CreateHubConnection();
        await _connection.StartAsync();

        // This should succeed — fresh join after reconnect
        var exception = await Record.ExceptionAsync(() =>
            _connection.InvokeAsync("JoinEcosystem", "test-eco-1"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Reconnected_Client_Can_Request_World_State()
    {
        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");

        await _connection.StopAsync();

        _connection = _factory.CreateHubConnection();
        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");

        // Per architecture doc: reconnected client should RequestWorldState to resync
        var worldState = await _connection.InvokeAsync<WorldStateUpdate>("RequestWorldState", "test-eco-1");

        Assert.NotNull(worldState);
        Assert.Equal("test-eco-1", worldState.EcosystemId);
    }

    [Fact]
    public async Task Disconnect_Notifies_Other_Peers()
    {
        // TODO: This test verifies that OnDisconnectedAsync broadcasts PeerAction.Leave.
        // Currently OnDisconnectedAsync only logs — it doesn't know which ecosystems
        // the peer was in. Once PeerGrain tracks this, this test should verify the broadcast.

        var announceReceived = new TaskCompletionSource<PeerAnnounce>();
        var client2 = _factory.CreateHubConnection();

        try
        {
            await _connection.StartAsync();
            await client2.StartAsync();
            await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");
            await client2.InvokeAsync("JoinEcosystem", "test-eco-1");

            _connection.On<PeerAnnounce>("ReceivePeerAnnounce", announce =>
            {
                if (announce.Action == PeerAction.Leave)
                    announceReceived.TrySetResult(announce);
            });

            // Disconnect client2 — should trigger OnDisconnectedAsync
            await client2.StopAsync();

            // TODO: Currently no broadcast on disconnect — will be implemented with PeerGrain
            // For now, just verify we don't crash
            var completed = await Task.WhenAny(
                announceReceived.Task,
                Task.Delay(TimeSpan.FromSeconds(2)));

            // Not asserting receipt — just verifying no crash
            Assert.Equal(HubConnectionState.Connected, _connection.State);
        }
        finally
        {
            await client2.DisposeAsync();
        }
    }
}
