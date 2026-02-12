using Microsoft.AspNetCore.SignalR.Client;
using Terrarium.Net;
using Xunit;

namespace Terrarium.SignalR.Tests;

/// <summary>
/// Tests for rate limiting per Heisenberg's architecture doc (Section 9).
/// Rate limits are per-connection, per-method.
///
/// NOTE: The hub does not yet implement rate limiting — these tests document
/// the expected behavior and will fail/skip until the implementation lands.
/// </summary>
public class RateLimitingTests : IClassFixture<TerrariumHubFactory>, IAsyncLifetime
{
    private readonly TerrariumHubFactory _factory;
    private HubConnection _connection = null!;

    public RateLimitingTests(TerrariumHubFactory factory)
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

    [Fact(Skip = "Rate limiting not yet implemented on hub — Sprint 7 TODO")]
    public async Task Teleport_Rate_Limit_10_Per_60_Seconds()
    {
        // Per architecture doc: TeleportCreature — 10 calls per 60 seconds
        var errors = new List<HubError>();

        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");

        _connection.On<HubError>("ReceiveError", error => errors.Add(error));

        for (int i = 0; i < 11; i++)
        {
            await _connection.InvokeAsync("TeleportCreature", new CreatureTeleport
            {
                TeleportId = Guid.NewGuid().ToString(),
                EcosystemId = "test-eco-1",
                OrganismId = $"organism-{i}",
                SpeciesAssemblyName = "Test, Version=1.0.0.0",
                SourcePeerId = _connection.ConnectionId!,
                StatePayload = "{}"
            });
        }

        // 11th call should trigger rate limiting
        await Task.Delay(500); // Allow error callback to arrive
        Assert.Contains(errors, e => e.Code == "RATE_LIMITED");
    }

    [Fact(Skip = "Rate limiting not yet implemented on hub — Sprint 7 TODO")]
    public async Task ReportPopulation_Rate_Limit_2_Per_60_Seconds()
    {
        // Per architecture doc: ReportPopulation — 2 calls per 60 seconds
        var errors = new List<HubError>();

        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");

        _connection.On<HubError>("ReceiveError", error => errors.Add(error));

        for (int i = 0; i < 3; i++)
        {
            await _connection.InvokeAsync("ReportPopulation", new PopulationReport
            {
                EcosystemId = "test-eco-1",
                TickNumber = i,
                Species = [],
                TotalOrganisms = 0
            });
        }

        await Task.Delay(500);
        Assert.Contains(errors, e => e.Code == "RATE_LIMITED");
    }

    [Fact(Skip = "Rate limiting not yet implemented on hub — Sprint 7 TODO")]
    public async Task RequestWorldState_Rate_Limit_5_Per_60_Seconds()
    {
        // Per architecture doc: RequestWorldState — 5 calls per 60 seconds
        var errors = new List<HubError>();

        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");

        _connection.On<HubError>("ReceiveError", error => errors.Add(error));

        for (int i = 0; i < 6; i++)
        {
            await _connection.InvokeAsync<WorldStateUpdate>("RequestWorldState", "test-eco-1");
        }

        await Task.Delay(500);
        Assert.Contains(errors, e => e.Code == "RATE_LIMITED");
    }

    [Fact(Skip = "Rate limiting not yet implemented on hub — Sprint 7 TODO")]
    public async Task RequestPeerList_Rate_Limit_2_Per_60_Seconds()
    {
        // Per architecture doc: RequestPeerList — 2 calls per 60 seconds
        var errors = new List<HubError>();

        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");

        _connection.On<HubError>("ReceiveError", error => errors.Add(error));

        for (int i = 0; i < 3; i++)
        {
            await _connection.InvokeAsync<PeerListResponse>("RequestPeerList", "test-eco-1");
        }

        await Task.Delay(500);
        Assert.Contains(errors, e => e.Code == "RATE_LIMITED");
    }

    [Fact(Skip = "Rate limiting not yet implemented on hub — Sprint 7 TODO")]
    public async Task Heartbeat_Rate_Limit_3_Per_60_Seconds()
    {
        // Per architecture doc: Heartbeat — 3 calls per 60 seconds
        var errors = new List<HubError>();

        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");

        _connection.On<HubError>("ReceiveError", error => errors.Add(error));

        for (int i = 0; i < 4; i++)
        {
            await _connection.InvokeAsync("Heartbeat", "test-eco-1");
        }

        await Task.Delay(500);
        Assert.Contains(errors, e => e.Code == "RATE_LIMITED");
    }

    [Fact(Skip = "Rate limiting not yet implemented on hub — Sprint 7 TODO")]
    public async Task Rate_Limit_Error_Contains_RetryAfterMs()
    {
        var errorReceived = new TaskCompletionSource<HubError>();

        await _connection.StartAsync();
        await _connection.InvokeAsync("JoinEcosystem", "test-eco-1");

        _connection.On<HubError>("ReceiveError", error =>
        {
            if (error.Code == "RATE_LIMITED")
                errorReceived.TrySetResult(error);
        });

        // Exceed teleport rate limit
        for (int i = 0; i < 11; i++)
        {
            await _connection.InvokeAsync("TeleportCreature", new CreatureTeleport
            {
                TeleportId = Guid.NewGuid().ToString(),
                EcosystemId = "test-eco-1",
                OrganismId = $"organism-{i}",
                SpeciesAssemblyName = "Test, Version=1.0.0.0",
                SourcePeerId = _connection.ConnectionId!,
                StatePayload = "{}"
            });
        }

        var error = await WaitWithTimeout(errorReceived.Task, TimeSpan.FromSeconds(5));

        Assert.True(error.IsTransient);
        Assert.NotNull(error.RetryAfterMs);
        Assert.True(error.RetryAfterMs > 0);
    }

    private static async Task<T> WaitWithTimeout<T>(Task<T> task, TimeSpan timeout)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeout));
        if (completed == task)
            return await task;

        throw new TimeoutException($"Operation did not complete within {timeout}");
    }
}
