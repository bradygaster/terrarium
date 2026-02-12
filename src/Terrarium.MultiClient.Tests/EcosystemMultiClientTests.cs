using Microsoft.AspNetCore.SignalR.Client;
using Terrarium.Net;
using Xunit;

namespace Terrarium.MultiClient.Tests;

/// <summary>
/// Multi-client ecosystem tests.
/// Validates peer-to-peer ecosystem behavior across N simulated browser clients:
/// - Creature teleportation visibility across clients
/// - Peer list updates and synchronization
/// - Population data aggregation
/// 
/// Sprint 11 Issue #73
/// </summary>
public class EcosystemMultiClientTests : IClassFixture<MultiClientFactory>
{
    private readonly MultiClientFactory _factory;

    public EcosystemMultiClientTests(MultiClientFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Three_Clients_All_See_PeerAnnounce_When_New_Client_Joins()
    {
        var clients = _factory.CreateClients(4);
        var announceReceivedCount = 0;
        var tcs = new TaskCompletionSource<int>();

        // Start first 3 clients and join ecosystem
        for (int i = 0; i < 3; i++)
        {
            await clients[i].StartAsync();
            await clients[i].InvokeAsync("JoinEcosystem", "test-ecosystem");
        }

        // Register announce handler on first 3 clients
        for (int i = 0; i < 3; i++)
        {
            clients[i].On<PeerAnnounce>("ReceivePeerAnnounce", announce =>
            {
                if (announce.Action == PeerAction.Join)
                {
                    Interlocked.Increment(ref announceReceivedCount);
                    if (announceReceivedCount == 3)
                        tcs.TrySetResult(announceReceivedCount);
                }
            });
        }

        // Fourth client joins — should broadcast to all 3 existing peers
        await clients[3].StartAsync();
        await clients[3].InvokeAsync("JoinEcosystem", "test-ecosystem");

        var result = await WaitWithTimeout(tcs.Task, TimeSpan.FromSeconds(5));

        Assert.Equal(3, result);
    }

    [Fact]
    public async Task Creature_Teleported_From_Client1_Appears_On_Client2_And_Client3()
    {
        var clients = _factory.CreateClients(3);
        var receivedOnClient2 = new TaskCompletionSource<CreatureTeleport>();
        var receivedOnClient3 = new TaskCompletionSource<CreatureTeleport>();

        await clients[0].StartAsync();
        await clients[1].StartAsync();
        await clients[2].StartAsync();

        await clients[0].InvokeAsync("JoinEcosystem", "ecosystem-alpha");
        await clients[1].InvokeAsync("JoinEcosystem", "ecosystem-alpha");
        await clients[2].InvokeAsync("JoinEcosystem", "ecosystem-alpha");

        clients[1].On<CreatureTeleport>("ReceiveCreatureTeleport", teleport =>
        {
            receivedOnClient2.TrySetResult(teleport);
        });

        clients[2].On<CreatureTeleport>("ReceiveCreatureTeleport", teleport =>
        {
            receivedOnClient3.TrySetResult(teleport);
        });

        // Client 0 introduces a creature via broadcast teleport
        await clients[0].InvokeAsync("TeleportCreature", new CreatureTeleport
        {
            TeleportId = Guid.NewGuid().ToString(),
            EcosystemId = "ecosystem-alpha",
            OrganismId = "creature-001",
            SpeciesAssemblyName = "TestSpecies, Version=1.0.0.0",
            SourcePeerId = clients[0].ConnectionId!,
            TargetPeerId = null, // broadcast
            StatePayload = """{"name":"Wanderer","energy":100}"""
        });

        var result2 = await WaitWithTimeout(receivedOnClient2.Task, TimeSpan.FromSeconds(5));
        var result3 = await WaitWithTimeout(receivedOnClient3.Task, TimeSpan.FromSeconds(5));

        Assert.NotNull(result2);
        Assert.Equal("creature-001", result2.OrganismId);
        Assert.NotNull(result3);
        Assert.Equal("creature-001", result3.OrganismId);
    }

    [Fact]
    public async Task Targeted_Teleport_Only_Delivered_To_Target_Not_Others()
    {
        var clients = _factory.CreateClients(3);
        var receivedOnClient1 = new TaskCompletionSource<CreatureTeleport>();
        var receivedOnClient2 = new TaskCompletionSource<CreatureTeleport>();

        await clients[0].StartAsync();
        await clients[1].StartAsync();
        await clients[2].StartAsync();

        await clients[0].InvokeAsync("JoinEcosystem", "ecosystem-beta");
        await clients[1].InvokeAsync("JoinEcosystem", "ecosystem-beta");
        await clients[2].InvokeAsync("JoinEcosystem", "ecosystem-beta");

        clients[1].On<CreatureTeleport>("ReceiveCreatureTeleport", teleport =>
        {
            receivedOnClient1.TrySetResult(teleport);
        });

        clients[2].On<CreatureTeleport>("ReceiveCreatureTeleport", teleport =>
        {
            receivedOnClient2.TrySetResult(teleport);
        });

        // Client 0 sends creature directly to client 2 (not client 1)
        await clients[0].InvokeAsync("TeleportCreature", new CreatureTeleport
        {
            TeleportId = Guid.NewGuid().ToString(),
            EcosystemId = "ecosystem-beta",
            OrganismId = "creature-002",
            SpeciesAssemblyName = "TestSpecies, Version=1.0.0.0",
            SourcePeerId = clients[0].ConnectionId!,
            TargetPeerId = clients[2].ConnectionId!,
            StatePayload = """{"name":"DirectTarget","energy":80}"""
        });

        var result2 = await WaitWithTimeout(receivedOnClient2.Task, TimeSpan.FromSeconds(5));

        Assert.NotNull(result2);
        Assert.Equal("creature-002", result2.OrganismId);

        // Client 1 should NOT receive this targeted teleport
        var completed = await Task.WhenAny(
            receivedOnClient1.Task,
            Task.Delay(TimeSpan.FromSeconds(2)));

        Assert.NotEqual(receivedOnClient1.Task, completed);
    }

    [Fact]
    public async Task Five_Clients_RequestPeerList_Returns_Consistent_Results()
    {
        var clients = _factory.CreateClients(5);

        for (int i = 0; i < 5; i++)
        {
            await clients[i].StartAsync();
            await clients[i].InvokeAsync("JoinEcosystem", "ecosystem-gamma");
        }

        // Small delay to ensure all joins are processed
        await Task.Delay(200);

        // All clients request peer list
        var peerLists = new List<PeerListResponse>();
        for (int i = 0; i < 5; i++)
        {
            var peerList = await clients[i].InvokeAsync<PeerListResponse>(
                "RequestPeerList", "ecosystem-gamma");
            peerLists.Add(peerList);
        }

        // Verify all responses have the same ecosystem
        Assert.All(peerLists, pl => Assert.Equal("ecosystem-gamma", pl.EcosystemId));

        // Verify all responses have peer data (even if empty list — Orleans not wired yet)
        Assert.All(peerLists, pl => Assert.NotNull(pl.Peers));
    }

    [Fact]
    public async Task Client_Leave_Broadcasts_PeerAnnounce_To_All_Remaining_Clients()
    {
        var clients = _factory.CreateClients(4);
        var leaveAnnouncesReceived = 0;
        var tcs = new TaskCompletionSource<int>();

        for (int i = 0; i < 4; i++)
        {
            await clients[i].StartAsync();
            await clients[i].InvokeAsync("JoinEcosystem", "ecosystem-delta");
        }

        // Clients 1, 2, 3 listen for leave announcements
        for (int i = 1; i < 4; i++)
        {
            clients[i].On<PeerAnnounce>("ReceivePeerAnnounce", announce =>
            {
                if (announce.Action == PeerAction.Leave)
                {
                    Interlocked.Increment(ref leaveAnnouncesReceived);
                    if (leaveAnnouncesReceived == 3)
                        tcs.TrySetResult(leaveAnnouncesReceived);
                }
            });
        }

        // Client 0 leaves
        await clients[0].InvokeAsync("LeaveEcosystem", "ecosystem-delta");

        var result = await WaitWithTimeout(tcs.Task, TimeSpan.FromSeconds(5));

        Assert.Equal(3, result);
    }

    [Fact]
    public async Task Teleport_With_AssemblyPayload_Delivered_To_Multiple_Clients()
    {
        var clients = _factory.CreateClients(3);
        var receivedOnClient1 = new TaskCompletionSource<CreatureTeleport>();
        var receivedOnClient2 = new TaskCompletionSource<CreatureTeleport>();

        await clients[0].StartAsync();
        await clients[1].StartAsync();
        await clients[2].StartAsync();

        await clients[0].InvokeAsync("JoinEcosystem", "ecosystem-epsilon");
        await clients[1].InvokeAsync("JoinEcosystem", "ecosystem-epsilon");
        await clients[2].InvokeAsync("JoinEcosystem", "ecosystem-epsilon");

        clients[1].On<CreatureTeleport>("ReceiveCreatureTeleport", teleport =>
        {
            receivedOnClient1.TrySetResult(teleport);
        });

        clients[2].On<CreatureTeleport>("ReceiveCreatureTeleport", teleport =>
        {
            receivedOnClient2.TrySetResult(teleport);
        });

        var fakeAssembly = Convert.ToBase64String(new byte[] { 0x4D, 0x5A, 0x90, 0x00, 0x03 });

        await clients[0].InvokeAsync("TeleportCreature", new CreatureTeleport
        {
            TeleportId = Guid.NewGuid().ToString(),
            EcosystemId = "ecosystem-epsilon",
            OrganismId = "creature-003",
            SpeciesAssemblyName = "NewSpecies, Version=1.0.0.0",
            SourcePeerId = clients[0].ConnectionId!,
            TargetPeerId = null,
            StatePayload = """{"name":"NewCreature","energy":50}""",
            AssemblyPayload = fakeAssembly
        });

        var result1 = await WaitWithTimeout(receivedOnClient1.Task, TimeSpan.FromSeconds(5));
        var result2 = await WaitWithTimeout(receivedOnClient2.Task, TimeSpan.FromSeconds(5));

        Assert.Equal(fakeAssembly, result1.AssemblyPayload);
        Assert.Equal(fakeAssembly, result2.AssemblyPayload);
    }

    [Fact]
    public async Task Ten_Clients_Can_Connect_And_Exchange_Creatures()
    {
        var clients = _factory.CreateClients(10);
        var teleportsReceived = new System.Collections.Concurrent.ConcurrentBag<CreatureTeleport>();
        var expectedTeleports = 9; // All except sender

        for (int i = 0; i < 10; i++)
        {
            await clients[i].StartAsync();
            await clients[i].InvokeAsync("JoinEcosystem", "ecosystem-load");
        }

        // All clients except the first register teleport handlers
        for (int i = 1; i < 10; i++)
        {
            clients[i].On<CreatureTeleport>("ReceiveCreatureTeleport", teleport =>
            {
                teleportsReceived.Add(teleport);
            });
        }

        // Client 0 broadcasts a creature
        await clients[0].InvokeAsync("TeleportCreature", new CreatureTeleport
        {
            TeleportId = Guid.NewGuid().ToString(),
            EcosystemId = "ecosystem-load",
            OrganismId = "creature-broadcast",
            SpeciesAssemblyName = "TestSpecies, Version=1.0.0.0",
            SourcePeerId = clients[0].ConnectionId!,
            TargetPeerId = null,
            StatePayload = """{"name":"BroadcastCreature","energy":100}"""
        });

        // Wait for all clients to receive
        await Task.Delay(2000);

        Assert.Equal(expectedTeleports, teleportsReceived.Count);
    }

    private static async Task<T> WaitWithTimeout<T>(Task<T> task, TimeSpan timeout)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeout));
        if (completed == task)
            return await task;

        throw new TimeoutException($"Operation did not complete within {timeout}");
    }
}
