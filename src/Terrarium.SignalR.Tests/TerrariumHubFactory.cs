using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Terrarium.Net;
using Xunit;

namespace Terrarium.SignalR.Tests;

/// <summary>
/// Test fixture that spins up a lightweight ASP.NET Core test server with
/// TerrariumHub mapped. Uses TestServer (no Terrarium.Server dependency)
/// so these tests work even while Mike's hub wiring is in progress (#50).
/// </summary>
public class TerrariumHubFactory : IAsyncLifetime
{
    private IHost _host = null!;
    private TestServer _server = null!;

    public async Task InitializeAsync()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddSignalR();
                    services.AddLogging();
                });
                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapHub<TerrariumHub>("/hub/terrarium");
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

    /// <summary>
    /// Creates a HubConnection targeting the test server's hub endpoint.
    /// </summary>
    public HubConnection CreateHubConnection()
    {
        return new HubConnectionBuilder()
            .WithUrl($"{_server.BaseAddress}hub/terrarium", options =>
            {
                options.HttpMessageHandlerFactory = _ => _server.CreateHandler();
            })
            .Build();
    }
}
