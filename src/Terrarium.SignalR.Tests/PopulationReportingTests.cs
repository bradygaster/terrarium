using Microsoft.AspNetCore.SignalR.Client;
using Terrarium.Net;
using Xunit;

namespace Terrarium.SignalR.Tests;

/// <summary>
/// Tests for population reporting.
/// Validates ReportPopulation and ReceivePopulationReport per architecture doc (Section 8).
/// </summary>
public class PopulationReportingTests : IClassFixture<TerrariumHubFactory>, IAsyncLifetime
{
    private readonly TerrariumHubFactory _factory;
    private HubConnection _reporter = null!;
    private HubConnection _listener = null!;

    public PopulationReportingTests(TerrariumHubFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync()
    {
        _reporter = _factory.CreateHubConnection();
        _listener = _factory.CreateHubConnection();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_reporter.State != HubConnectionState.Disconnected)
            await _reporter.StopAsync();
        await _reporter.DisposeAsync();

        if (_listener.State != HubConnectionState.Disconnected)
            await _listener.StopAsync();
        await _listener.DisposeAsync();
    }

    [Fact]
    public async Task ReportPopulation_Broadcasts_To_Others_In_Ecosystem()
    {
        var reportReceived = new TaskCompletionSource<PopulationReport>();

        await _reporter.StartAsync();
        await _listener.StartAsync();
        await _reporter.InvokeAsync("JoinEcosystem", "test-eco-1");
        await _listener.InvokeAsync("JoinEcosystem", "test-eco-1");

        _listener.On<PopulationReport>("ReceivePopulationReport", report =>
        {
            reportReceived.TrySetResult(report);
        });

        var populationReport = new PopulationReport
        {
            EcosystemId = "test-eco-1",
            TickNumber = 42,
            Species = new List<SpeciesPopulation>
            {
                new() { SpeciesName = "TestBeetle", Population = 15, Births = 3, Deaths = 1 },
                new() { SpeciesName = "TestScorpion", Population = 8, Births = 1, Deaths = 2 }
            },
            TotalOrganisms = 23
        };

        await _reporter.InvokeAsync("ReportPopulation", populationReport);

        var result = await WaitWithTimeout(reportReceived.Task, TimeSpan.FromSeconds(5));

        Assert.NotNull(result);
        Assert.Equal("test-eco-1", result.EcosystemId);
        Assert.Equal(42, result.TickNumber);
        Assert.Equal(23, result.TotalOrganisms);
        Assert.Equal(2, result.Species.Count);
    }

    [Fact]
    public async Task ReportPopulation_Contains_Per_Species_Data()
    {
        var reportReceived = new TaskCompletionSource<PopulationReport>();

        await _reporter.StartAsync();
        await _listener.StartAsync();
        await _reporter.InvokeAsync("JoinEcosystem", "test-eco-1");
        await _listener.InvokeAsync("JoinEcosystem", "test-eco-1");

        _listener.On<PopulationReport>("ReceivePopulationReport", report =>
        {
            reportReceived.TrySetResult(report);
        });

        await _reporter.InvokeAsync("ReportPopulation", new PopulationReport
        {
            EcosystemId = "test-eco-1",
            TickNumber = 100,
            Species = new List<SpeciesPopulation>
            {
                new() { SpeciesName = "PlantA", Population = 50, Births = 10, Deaths = 5 }
            },
            TotalOrganisms = 50
        });

        var result = await WaitWithTimeout(reportReceived.Task, TimeSpan.FromSeconds(5));

        var species = Assert.Single(result.Species);
        Assert.Equal("PlantA", species.SpeciesName);
        Assert.Equal(50, species.Population);
        Assert.Equal(10, species.Births);
        Assert.Equal(5, species.Deaths);
    }

    [Fact]
    public async Task Reporter_Does_Not_Receive_Own_Report()
    {
        var selfReceived = new TaskCompletionSource<PopulationReport>();

        await _reporter.StartAsync();
        await _reporter.InvokeAsync("JoinEcosystem", "test-eco-1");

        _reporter.On<PopulationReport>("ReceivePopulationReport", report =>
        {
            selfReceived.TrySetResult(report);
        });

        await _reporter.InvokeAsync("ReportPopulation", new PopulationReport
        {
            EcosystemId = "test-eco-1",
            TickNumber = 1,
            Species = [],
            TotalOrganisms = 0
        });

        // Reporter should NOT receive its own report (OthersInGroup)
        var completed = await Task.WhenAny(
            selfReceived.Task,
            Task.Delay(TimeSpan.FromSeconds(2)));

        Assert.NotEqual(selfReceived.Task, completed);
    }

    [Fact]
    public async Task Population_Report_Not_Sent_To_Different_Ecosystem()
    {
        var reportReceived = new TaskCompletionSource<PopulationReport>();

        await _reporter.StartAsync();
        await _listener.StartAsync();
        await _reporter.InvokeAsync("JoinEcosystem", "eco-A");
        await _listener.InvokeAsync("JoinEcosystem", "eco-B");

        _listener.On<PopulationReport>("ReceivePopulationReport", report =>
        {
            reportReceived.TrySetResult(report);
        });

        await _reporter.InvokeAsync("ReportPopulation", new PopulationReport
        {
            EcosystemId = "eco-A",
            TickNumber = 1,
            Species = [],
            TotalOrganisms = 0
        });

        var completed = await Task.WhenAny(
            reportReceived.Task,
            Task.Delay(TimeSpan.FromSeconds(2)));

        Assert.NotEqual(reportReceived.Task, completed);
    }

    private static async Task<T> WaitWithTimeout<T>(Task<T> task, TimeSpan timeout)
    {
        var completed = await Task.WhenAny(task, Task.Delay(timeout));
        if (completed == task)
            return await task;

        throw new TimeoutException($"Operation did not complete within {timeout}");
    }
}
