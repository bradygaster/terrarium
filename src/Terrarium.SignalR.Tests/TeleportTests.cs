using Microsoft.AspNetCore.SignalR.Client;
using Terrarium.Net;
using Xunit;

namespace Terrarium.SignalR.Tests;

/// <summary>
/// Tests for creature teleportation.
/// Validates the single-call teleport flow per architecture doc (Section 5).
/// </summary>
public class TeleportTests : IClassFixture<TerrariumHubFactory>, IAsyncLifetime
{
    private readonly TerrariumHubFactory _factory;
    private HubConnection _source = null!;
    private HubConnection _target = null!;

    public TeleportTests(TerrariumHubFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync()
    {
        _source = _factory.CreateHubConnection();
        _target = _factory.CreateHubConnection();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_source.State != HubConnectionState.Disconnected)
            await _source.StopAsync();
        await _source.DisposeAsync();

        if (_target.State != HubConnectionState.Disconnected)
            await _target.StopAsync();
        await _target.DisposeAsync();
    }

    [Fact]
    public async Task Teleport_With_TargetPeerId_Delivers_To_Target()
    {
        var teleportReceived = new TaskCompletionSource<CreatureTeleport>();

        await _source.StartAsync();
        await _target.StartAsync();
        await _source.InvokeAsync("JoinEcosystem", "test-eco-1");
        await _target.InvokeAsync("JoinEcosystem", "test-eco-1");

        _target.On<CreatureTeleport>("ReceiveCreatureTeleport", teleport =>
        {
            teleportReceived.TrySetResult(teleport);
        });

        await _source.InvokeAsync("TeleportCreature", new CreatureTeleport
        {
            TeleportId = Guid.NewGuid().ToString(),
            EcosystemId = "test-eco-1",
            OrganismId = "organism-1",
            SpeciesAssemblyName = "TestSpecies, Version=1.0.0.0",
            SourcePeerId = _source.ConnectionId!,
            TargetPeerId = _target.ConnectionId!,
            StatePayload = """{"name":"TestCreature","energy":100}"""
        });

        var result = await WaitWithTimeout(teleportReceived.Task, TimeSpan.FromSeconds(5));

        Assert.NotNull(result);
        Assert.Equal("organism-1", result.OrganismId);
        Assert.Equal("TestSpecies, Version=1.0.0.0", result.SpeciesAssemblyName);
    }

    [Fact]
    public async Task Teleport_Without_Target_Broadcasts_To_Ecosystem()
    {
        var teleportReceived = new TaskCompletionSource<CreatureTeleport>();

        await _source.StartAsync();
        await _target.StartAsync();
        await _source.InvokeAsync("JoinEcosystem", "test-eco-1");
        await _target.InvokeAsync("JoinEcosystem", "test-eco-1");

        _target.On<CreatureTeleport>("ReceiveCreatureTeleport", teleport =>
        {
            teleportReceived.TrySetResult(teleport);
        });

        // No TargetPeerId — current hub broadcasts to others in group
        await _source.InvokeAsync("TeleportCreature", new CreatureTeleport
        {
            TeleportId = Guid.NewGuid().ToString(),
            EcosystemId = "test-eco-1",
            OrganismId = "organism-2",
            SpeciesAssemblyName = "TestSpecies, Version=1.0.0.0",
            SourcePeerId = _source.ConnectionId!,
            TargetPeerId = null,
            StatePayload = """{"name":"WanderingCreature","energy":75}"""
        });

        var result = await WaitWithTimeout(teleportReceived.Task, TimeSpan.FromSeconds(5));

        Assert.NotNull(result);
        Assert.Equal("organism-2", result.OrganismId);
    }

    [Fact]
    public async Task Teleport_With_Assembly_Payload_Is_Delivered()
    {
        var teleportReceived = new TaskCompletionSource<CreatureTeleport>();

        await _source.StartAsync();
        await _target.StartAsync();
        await _source.InvokeAsync("JoinEcosystem", "test-eco-1");
        await _target.InvokeAsync("JoinEcosystem", "test-eco-1");

        _target.On<CreatureTeleport>("ReceiveCreatureTeleport", teleport =>
        {
            teleportReceived.TrySetResult(teleport);
        });

        var fakeAssemblyBytes = Convert.ToBase64String(new byte[] { 0x4D, 0x5A, 0x90, 0x00 });

        await _source.InvokeAsync("TeleportCreature", new CreatureTeleport
        {
            TeleportId = Guid.NewGuid().ToString(),
            EcosystemId = "test-eco-1",
            OrganismId = "organism-3",
            SpeciesAssemblyName = "NewSpecies, Version=1.0.0.0",
            SourcePeerId = _source.ConnectionId!,
            TargetPeerId = _target.ConnectionId!,
            StatePayload = """{"name":"NewCreature","energy":50}""",
            AssemblyPayload = fakeAssemblyBytes
        });

        var result = await WaitWithTimeout(teleportReceived.Task, TimeSpan.FromSeconds(5));

        Assert.NotNull(result);
        Assert.Equal(fakeAssemblyBytes, result.AssemblyPayload);
    }

    [Fact]
    public async Task Teleport_Carries_Idempotency_Key()
    {
        var teleportReceived = new TaskCompletionSource<CreatureTeleport>();
        var teleportId = Guid.NewGuid().ToString();

        await _source.StartAsync();
        await _target.StartAsync();
        await _source.InvokeAsync("JoinEcosystem", "test-eco-1");
        await _target.InvokeAsync("JoinEcosystem", "test-eco-1");

        _target.On<CreatureTeleport>("ReceiveCreatureTeleport", teleport =>
        {
            teleportReceived.TrySetResult(teleport);
        });

        await _source.InvokeAsync("TeleportCreature", new CreatureTeleport
        {
            TeleportId = teleportId,
            EcosystemId = "test-eco-1",
            OrganismId = "organism-4",
            SpeciesAssemblyName = "TestSpecies, Version=1.0.0.0",
            SourcePeerId = _source.ConnectionId!,
            TargetPeerId = _target.ConnectionId!,
            StatePayload = """{"name":"TrackedCreature"}"""
        });

        var result = await WaitWithTimeout(teleportReceived.Task, TimeSpan.FromSeconds(5));

        Assert.Equal(teleportId, result.TeleportId);
    }

    [Fact]
    public async Task Teleport_To_Nonexistent_Peer_Does_Not_Crash_Hub()
    {
        await _source.StartAsync();
        await _source.InvokeAsync("JoinEcosystem", "test-eco-1");

        // Target is a fake connection ID that doesn't exist.
        // Current hub silently sends via Clients.Client() — no error expected.
        // TODO: Once Orleans validation is wired, this should ReceiveError with TELEPORT_FAILED.
        var exception = await Record.ExceptionAsync(() =>
            _source.InvokeAsync("TeleportCreature", new CreatureTeleport
            {
                TeleportId = Guid.NewGuid().ToString(),
                EcosystemId = "test-eco-1",
                OrganismId = "organism-ghost",
                SpeciesAssemblyName = "TestSpecies, Version=1.0.0.0",
                SourcePeerId = _source.ConnectionId!,
                TargetPeerId = "nonexistent-connection-id",
                StatePayload = """{"name":"Ghost"}"""
            }));

        // Hub should NOT throw — architecture says never throw from hub methods
        Assert.Null(exception);
    }

    [Fact]
    public async Task Source_Does_Not_Receive_Own_Broadcast_Teleport()
    {
        var selfReceived = new TaskCompletionSource<CreatureTeleport>();

        await _source.StartAsync();
        await _source.InvokeAsync("JoinEcosystem", "test-eco-1");

        _source.On<CreatureTeleport>("ReceiveCreatureTeleport", teleport =>
        {
            selfReceived.TrySetResult(teleport);
        });

        await _source.InvokeAsync("TeleportCreature", new CreatureTeleport
        {
            TeleportId = Guid.NewGuid().ToString(),
            EcosystemId = "test-eco-1",
            OrganismId = "organism-self",
            SpeciesAssemblyName = "TestSpecies, Version=1.0.0.0",
            SourcePeerId = _source.ConnectionId!,
            TargetPeerId = null,
            StatePayload = """{"name":"SelfTest"}"""
        });

        // Source should NOT receive its own broadcast (OthersInGroup)
        var completed = await Task.WhenAny(
            selfReceived.Task,
            Task.Delay(TimeSpan.FromSeconds(2)));

        Assert.NotEqual(selfReceived.Task, completed);
    }

    private static async Task<T> WaitWithTimeout<T>(Task<T> task, TimeSpan timeout)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeout));
        if (completed == task)
            return await task;

        throw new TimeoutException($"Operation did not complete within {timeout}");
    }
}
