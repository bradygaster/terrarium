using Microsoft.AspNetCore.SignalR.Client;
using Terrarium.Net;
using Xunit;

namespace Terrarium.SignalR.Tests;

/// <summary>
/// Tests for peer registration and discovery.
/// Validates PeerAnnounce broadcast and RequestPeerList per architecture doc (Section 6).
/// </summary>
public class PeerDiscoveryTests : IClassFixture<TerrariumHubFactory>, IAsyncLifetime
{
    private readonly TerrariumHubFactory _factory;
    private HubConnection _client1 = null!;
    private HubConnection _client2 = null!;

    public PeerDiscoveryTests(TerrariumHubFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync()
    {
        _client1 = _factory.CreateHubConnection();
        _client2 = _factory.CreateHubConnection();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_client1.State != HubConnectionState.Disconnected)
            await _client1.StopAsync();
        await _client1.DisposeAsync();

        if (_client2.State != HubConnectionState.Disconnected)
            await _client2.StopAsync();
        await _client2.DisposeAsync();
    }

    [Fact]
    public async Task Peer_Join_Broadcasts_PeerAnnounce_To_Others()
    {
        var announceReceived = new TaskCompletionSource<PeerAnnounce>();

        await _client1.StartAsync();
        await _client1.InvokeAsync("JoinEcosystem", "test-eco-1");

        _client1.On<PeerAnnounce>("ReceivePeerAnnounce", announce =>
        {
            announceReceived.TrySetResult(announce);
        });

        await _client2.StartAsync();
        await _client2.InvokeAsync("JoinEcosystem", "test-eco-1");

        var result = await WaitWithTimeout(announceReceived.Task, TimeSpan.FromSeconds(5));

        Assert.NotNull(result);
        Assert.Equal("test-eco-1", result.EcosystemId);
        Assert.Equal(PeerAction.Join, result.Action);
    }

    [Fact]
    public async Task Peer_Leave_Broadcasts_PeerAnnounce_To_Others()
    {
        var announceReceived = new TaskCompletionSource<PeerAnnounce>();

        await _client1.StartAsync();
        await _client1.InvokeAsync("JoinEcosystem", "test-eco-1");

        await _client2.StartAsync();
        await _client2.InvokeAsync("JoinEcosystem", "test-eco-1");

        // Register handler AFTER both have joined — skip join announcements
        _client1.On<PeerAnnounce>("ReceivePeerAnnounce", announce =>
        {
            if (announce.Action == PeerAction.Leave)
                announceReceived.TrySetResult(announce);
        });

        await _client2.InvokeAsync("LeaveEcosystem", "test-eco-1");

        var result = await WaitWithTimeout(announceReceived.Task, TimeSpan.FromSeconds(5));

        Assert.NotNull(result);
        Assert.Equal(PeerAction.Leave, result.Action);
    }

    [Fact]
    public async Task AnnouncePeer_Broadcasts_To_Others_In_Ecosystem()
    {
        var announceReceived = new TaskCompletionSource<PeerAnnounce>();

        await _client1.StartAsync();
        await _client1.InvokeAsync("JoinEcosystem", "test-eco-1");

        await _client2.StartAsync();
        await _client2.InvokeAsync("JoinEcosystem", "test-eco-1");

        _client1.On<PeerAnnounce>("ReceivePeerAnnounce", announce =>
        {
            if (announce.Version is not null)
                announceReceived.TrySetResult(announce);
        });

        await _client2.InvokeAsync("AnnouncePeer", new PeerAnnounce
        {
            PeerId = "client2",
            EcosystemId = "test-eco-1",
            Action = PeerAction.Join,
            Version = "1.0.0",
            Channel = "stable"
        });

        var result = await WaitWithTimeout(announceReceived.Task, TimeSpan.FromSeconds(5));

        Assert.NotNull(result);
        Assert.Equal("1.0.0", result.Version);
    }

    [Fact]
    public async Task RequestPeerList_Returns_Response()
    {
        await _client1.StartAsync();
        await _client1.InvokeAsync("JoinEcosystem", "test-eco-1");

        var peerList = await _client1.InvokeAsync<PeerListResponse>("RequestPeerList", "test-eco-1");

        Assert.NotNull(peerList);
        Assert.Equal("test-eco-1", peerList.EcosystemId);
        // TODO: When Orleans grains are wired up, verify the peer list contains actual peers
        Assert.NotNull(peerList.Peers);
    }

    [Fact]
    public async Task Peer_Announce_Not_Sent_To_Different_Ecosystem()
    {
        var announceReceived = new TaskCompletionSource<PeerAnnounce>();

        await _client1.StartAsync();
        await _client1.InvokeAsync("JoinEcosystem", "eco-A");

        _client1.On<PeerAnnounce>("ReceivePeerAnnounce", announce =>
        {
            announceReceived.TrySetResult(announce);
        });

        await _client2.StartAsync();
        await _client2.InvokeAsync("JoinEcosystem", "eco-B"); // Different ecosystem

        // Should NOT receive announcement — different ecosystems
        var completed = await Task.WhenAny(
            announceReceived.Task,
            Task.Delay(TimeSpan.FromSeconds(2)));

        Assert.NotEqual(announceReceived.Task, completed);
    }

    [Fact]
    public async Task Duplicate_Peer_Registration_Does_Not_Throw()
    {
        await _client1.StartAsync();

        // Joining the same ecosystem twice should not throw
        await _client1.InvokeAsync("JoinEcosystem", "test-eco-1");
        await _client1.InvokeAsync("JoinEcosystem", "test-eco-1");

        Assert.Equal(HubConnectionState.Connected, _client1.State);
    }

    private static async Task<T> WaitWithTimeout<T>(Task<T> task, TimeSpan timeout)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeout));
        if (completed == task)
            return await task;

        throw new TimeoutException($"Operation did not complete within {timeout}");
    }
}
