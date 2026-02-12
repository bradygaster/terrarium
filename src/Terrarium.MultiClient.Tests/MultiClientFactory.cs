using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Terrarium.Net;
using Xunit;

namespace Terrarium.MultiClient.Tests;

/// <summary>
/// Test fixture that spins up a TestServer and creates N SignalR hub connections
/// simulating N browser clients connected to the same ecosystem.
/// Uses TestServer pattern (no Terrarium.Server dependency) for build safety.
/// </summary>
public class MultiClientFactory : IAsyncLifetime
{
    private IHost _host = null!;
    private TestServer _server = null!;
    private readonly List<HubConnection> _connections = new();

    public async Task InitializeAsync()
    {
        _host = new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddSignalR(options =>
                    {
                        options.MaximumReceiveMessageSize = 512 * 1024;
                    });
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
        foreach (var connection in _connections)
        {
            if (connection.State != HubConnectionState.Disconnected)
                await connection.StopAsync();
            await connection.DisposeAsync();
        }

        await _host.StopAsync();
        _host.Dispose();
    }

    /// <summary>
    /// Creates N hub connections simulating N browser clients.
    /// </summary>
    public List<HubConnection> CreateClients(int count)
    {
        var clients = new List<HubConnection>();

        for (int i = 0; i < count; i++)
        {
            var connection = new HubConnectionBuilder()
                .WithUrl($"{_server.BaseAddress}hub/terrarium", options =>
                {
                    options.HttpMessageHandlerFactory = _ => _server.CreateHandler();
                })
                .Build();

            _connections.Add(connection);
            clients.Add(connection);
        }

        return clients;
    }
}
