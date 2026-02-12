using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Terrarium.Server.Workers;

/// <summary>
/// Background service that tracks Azure SignalR Service scaling state.
/// Logs when the service is running with Azure SignalR for horizontal scaling.
/// </summary>
public sealed class SignalRScalingService : IHostedService
{
    private readonly ILogger<SignalRScalingService> _logger;

    public SignalRScalingService(ILogger<SignalRScalingService> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Azure SignalR Service enabled — multi-server deployment with sticky sessions. "
            + "TerrariumHub will route messages through Azure SignalR backplane.");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
