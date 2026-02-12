using Microsoft.Extensions.Logging;
using Terrarium.Orleans.Models;

namespace Terrarium.Orleans.Grains;

/// <summary>
/// Orleans grain that holds state for a single connected peer.
/// Used for heartbeat tracking and peer discovery.
/// </summary>
public class PeerGrain : Grain, IPeerGrain
{
    private readonly ILogger<PeerGrain> _logger;

    private string? _endpoint;
    private string? _version;
    private DateTimeOffset _lastHeartbeat;
    private DateTimeOffset _connectedAt;

    public PeerGrain(ILogger<PeerGrain> logger)
    {
        _logger = logger;
    }

    public Task RegisterAsync(string? endpoint, string? version)
    {
        _endpoint = endpoint;
        _version = version;
        _connectedAt = DateTimeOffset.UtcNow;
        _lastHeartbeat = _connectedAt;
        _logger.LogInformation("Peer {PeerId} registered (endpoint={Endpoint}, version={Version})",
            this.GetPrimaryKeyString(), endpoint, version);
        return Task.CompletedTask;
    }

    public Task HeartbeatAsync()
    {
        _lastHeartbeat = DateTimeOffset.UtcNow;
        _logger.LogDebug("Peer {PeerId} heartbeat at {Time}",
            this.GetPrimaryKeyString(), _lastHeartbeat);
        return Task.CompletedTask;
    }

    public Task<PeerInfo> GetInfoAsync()
    {
        var info = new PeerInfo
        {
            PeerId = this.GetPrimaryKeyString(),
            Endpoint = _endpoint,
            Version = _version,
            LastHeartbeat = _lastHeartbeat,
            ConnectedAt = _connectedAt
        };
        return Task.FromResult(info);
    }
}
