using Microsoft.AspNetCore.SignalR.Client;
using Terrarium.Net;
using Xunit;

namespace Terrarium.SignalR.Tests;

/// <summary>
/// Tests for RequestWorldState hub method.
/// Validates WorldStateUpdate response per architecture doc (Section 2).
/// </summary>
public class WorldStateTests : IClassFixture<TerrariumHubFactory>, IAsyncLifetime
{
    private readonly TerrariumHubFactory _factory;
    private HubConnection _connection = null!;

    public WorldStateTests(TerrariumHubFactory factory)
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
    public async Task RequestWorldState_Returns_Matching_EcosystemId()
    {
        await _connection.StartAsync();

        var worldState = await _connection.InvokeAsync<WorldStateUpdate>("RequestWorldState", "my-eco");

        Assert.Equal("my-eco", worldState.EcosystemId);
    }

    [Fact]
    public async Task RequestWorldState_Returns_Timestamp()
    {
        var before = DateTimeOffset.UtcNow;
        await _connection.StartAsync();

        var worldState = await _connection.InvokeAsync<WorldStateUpdate>("RequestWorldState", "test-eco-1");

        // Timestamp should be recent (within a few seconds of now)
        Assert.True(worldState.Timestamp >= before.AddSeconds(-5));
        Assert.True(worldState.Timestamp <= DateTimeOffset.UtcNow.AddSeconds(5));
    }

    [Fact]
    public async Task RequestWorldState_Is_Caller_Only()
    {
        var client2 = _factory.CreateHubConnection();
        var unexpectedUpdate = new TaskCompletionSource<WorldStateUpdate>();

        try
        {
            await _connection.StartAsync();
            await client2.StartAsync();
            await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");
            await client2.InvokeAsync("JoinEcosystem", "test-eco-1");

            client2.On<WorldStateUpdate>("ReceiveWorldStateUpdate", update =>
            {
                unexpectedUpdate.TrySetResult(update);
            });

            // Only the caller should receive the response
            await _connection.InvokeAsync<WorldStateUpdate>("RequestWorldState", "test-eco-1");

            // Client2 should NOT receive a world state update from client1's request
            var completed = await Task.WhenAny(
                unexpectedUpdate.Task,
                Task.Delay(TimeSpan.FromSeconds(2)));

            Assert.NotEqual(unexpectedUpdate.Task, completed);
        }
        finally
        {
            if (client2.State != HubConnectionState.Disconnected)
                await client2.StopAsync();
            await client2.DisposeAsync();
        }
    }
}
