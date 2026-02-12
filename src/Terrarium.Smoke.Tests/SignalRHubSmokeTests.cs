using Microsoft.AspNetCore.SignalR.Client;
using Terrarium.Net;
using Xunit;

namespace Terrarium.Smoke.Tests;

/// <summary>
/// Smoke tests verifying SignalR hub accepts connections and basic operations work.
/// Uses the shared TerrariumServerFactory test server.
/// </summary>
public class SignalRHubSmokeTests : IClassFixture<TerrariumServerFactory>
{
    private readonly TerrariumServerFactory _factory;

    public SignalRHubSmokeTests(TerrariumServerFactory factory)
    {
        _factory = factory;
    }

    private HubConnection CreateConnection()
    {
        return new HubConnectionBuilder()
            .WithUrl($"{_factory.Server.BaseAddress}hubs/terrarium", options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();
    }

    [Fact]
    public async Task Hub_Accepts_Connection()
    {
        await using var connection = CreateConnection();

        await connection.StartAsync();

        Assert.Equal(HubConnectionState.Connected, connection.State);
    }

    [Fact]
    public async Task Hub_Accepts_JoinEcosystem()
    {
        await using var connection = CreateConnection();
        await connection.StartAsync();

        var exception = await Record.ExceptionAsync(() =>
            connection.InvokeAsync("JoinEcosystem", "smoke-test-eco"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Hub_Accepts_Heartbeat()
    {
        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinEcosystem", "smoke-test-eco");

        var exception = await Record.ExceptionAsync(() =>
            connection.InvokeAsync("Heartbeat", "smoke-test-eco"));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Hub_Returns_WorldState()
    {
        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinEcosystem", "smoke-test-eco");

        var result = await connection.InvokeAsync<WorldStateUpdate>(
            "RequestWorldState", "smoke-test-eco");

        Assert.NotNull(result);
        Assert.Equal("smoke-test-eco", result.EcosystemId);
    }

    [Fact]
    public async Task Hub_Returns_PeerList()
    {
        await using var connection = CreateConnection();
        await connection.StartAsync();
        await connection.InvokeAsync("JoinEcosystem", "smoke-test-eco");

        var result = await connection.InvokeAsync<PeerListResponse>(
            "RequestPeerList", "smoke-test-eco");

        Assert.NotNull(result);
        Assert.Equal("smoke-test-eco", result.EcosystemId);
    }

    [Fact]
    public async Task Hub_Disconnects_Cleanly()
    {
        var connection = CreateConnection();
        await connection.StartAsync();
        Assert.Equal(HubConnectionState.Connected, connection.State);

        await connection.StopAsync();

        Assert.Equal(HubConnectionState.Disconnected, connection.State);
        await connection.DisposeAsync();
    }
}
