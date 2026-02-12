using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Terrarium.ServiceDefaults;
using Xunit;

namespace Terrarium.Smoke.Tests;

/// <summary>
/// Smoke tests verifying the DI container resolves all registered services.
/// Ensures no missing registrations or circular dependencies at startup.
/// </summary>
public class DiContainerSmokeTests : IClassFixture<TerrariumServerFactory>
{
    private readonly IServiceProvider _services;

    public DiContainerSmokeTests(TerrariumServerFactory factory)
    {
        _services = factory.Services;
    }

    [Fact]
    public void Resolves_MemoryCache()
    {
        var cache = _services.GetService<IMemoryCache>();

        Assert.NotNull(cache);
    }

    [Fact]
    public void Resolves_HealthCheckService()
    {
        var health = _services.GetService<HealthCheckService>();

        Assert.NotNull(health);
    }

    [Fact]
    public void Resolves_TerrariumTelemetry()
    {
        var telemetry = _services.GetService<TerrariumTelemetry>();

        Assert.NotNull(telemetry);
    }

    [Fact]
    public async Task HealthChecks_Report_Healthy()
    {
        var healthService = _services.GetRequiredService<HealthCheckService>();

        var report = await healthService.CheckHealthAsync();

        Assert.Equal(HealthStatus.Healthy, report.Status);
    }
}
