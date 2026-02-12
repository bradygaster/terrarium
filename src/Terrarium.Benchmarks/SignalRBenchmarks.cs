using System.Collections.Concurrent;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.AspNetCore.SignalR.Client;

namespace Terrarium.Benchmarks;

/// <summary>
/// Benchmarks for SignalR hub performance:
/// - Concurrent connection capacity
/// - Message throughput (messages/second)
/// - Teleport latency
/// 
/// Sprint 11 Issue #77
/// 
/// IMPORTANT: These benchmarks require a running Terrarium server.
/// Start the server before running: dotnet run --project src/Terrarium.Server
/// </summary>
[SimpleJob(RuntimeMoniker.Net90)]
[MemoryDiagnoser]
public class SignalRBenchmarks
{
    private const string ServerUrl = "https://localhost:7001/hub/terrarium";
    private readonly List<HubConnection> _connections = new();

    [GlobalCleanup]
    public async Task Cleanup()
    {
        foreach (var conn in _connections)
        {
            if (conn.State != HubConnectionState.Disconnected)
                await conn.StopAsync();
            await conn.DisposeAsync();
        }
    }

    [Benchmark]
    public async Task Connect_10_Clients()
    {
        await ConnectClients(10);
    }

    [Benchmark]
    public async Task Connect_50_Clients()
    {
        await ConnectClients(50);
    }

    [Benchmark]
    public async Task Connect_100_Clients()
    {
        await ConnectClients(100);
    }

    [Benchmark]
    public async Task Teleport_Message_Throughput_100_Messages()
    {
        var clients = await ConnectClients(5);

        var sw = Stopwatch.StartNew();

        foreach (var client in clients)
        {
            await client.InvokeAsync("JoinEcosystem", "benchmark-ecosystem");
        }

        // Send 100 teleport messages from first client
        for (int i = 0; i < 100; i++)
        {
            await clients[0].InvokeAsync("TeleportCreature", new
            {
                TeleportId = Guid.NewGuid().ToString(),
                EcosystemId = "benchmark-ecosystem",
                OrganismId = $"creature-{i}",
                SpeciesAssemblyName = "TestSpecies, Version=1.0.0.0",
                SourcePeerId = clients[0].ConnectionId,
                TargetPeerId = (string?)null,
                StatePayload = """{"energy":100}"""
            });
        }

        sw.Stop();
        var messagesPerSecond = 100.0 / sw.Elapsed.TotalSeconds;
        Console.WriteLine($"Throughput: {messagesPerSecond:F2} messages/sec");
    }

    [Benchmark]
    public async Task Peer_Announce_Fanout_50_Clients()
    {
        var clients = await ConnectClients(50);

        // All clients join ecosystem
        foreach (var client in clients)
        {
            await client.InvokeAsync("JoinEcosystem", "fanout-test");
        }

        var sw = Stopwatch.StartNew();

        // Broadcast peer announce — should fan out to all 49 others
        await clients[0].InvokeAsync("AnnouncePeer", new
        {
            PeerId = clients[0].ConnectionId,
            EcosystemId = "fanout-test",
            Action = "Join",
            Version = "1.0.0",
            Channel = "stable"
        });

        sw.Stop();
        Console.WriteLine($"Fanout latency: {sw.ElapsedMilliseconds}ms");
    }

    private async Task<List<HubConnection>> ConnectClients(int count)
    {
        var clients = new List<HubConnection>();

        for (int i = 0; i < count; i++)
        {
            var connection = new HubConnectionBuilder()
                .WithUrl(ServerUrl)
                .Build();

            await connection.StartAsync();
            clients.Add(connection);
            _connections.Add(connection);
        }

        return clients;
    }
}
