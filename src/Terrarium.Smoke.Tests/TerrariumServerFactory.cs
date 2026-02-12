using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Terrarium.Net;
using Terrarium.ServiceDefaults;
using Xunit;

namespace Terrarium.Smoke.Tests;

/// <summary>
/// Test fixture that spins up a lightweight Terrarium server with TestServer.
/// Mirrors Terrarium.Server's Program.cs setup without the broken SpeciesEndpoints
/// (pre-existing CS0103 errors in SpeciesEndpoints.cs block direct project reference).
/// </summary>
public class TerrariumServerFactory : IAsyncLifetime
{
    private IHost _host = null!;
    private TestServer _server = null!;

    public TestServer Server => _server;
    public IServiceProvider Services => _host.Services;

    public async Task InitializeAsync()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddMemoryCache();
                    services.AddLogging();
                    services.AddSignalR(options =>
                    {
                        options.MaximumReceiveMessageSize = 512 * 1024;
                    });
                    services.AddHealthChecks()
                        .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);
                    services.AddSingleton<TerrariumTelemetry>();
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapHealthChecks("/health");
                        endpoints.MapHealthChecks("/alive", new HealthCheckOptions
                        {
                            Predicate = r => r.Tags.Contains("live")
                        });
                        endpoints.MapHub<TerrariumHub>("/hubs/terrarium");
                        endpoints.MapGet("/", () => "Terrarium Server");
                    });
                });
            })
            .Build();

        await _host.StartAsync();
        _server = _host.GetTestServer();
    }

    public async Task DisposeAsync()
    {
        await _host.StopAsync();
        _host.Dispose();
    }

    public HttpClient CreateClient() => _server.CreateClient();
}
