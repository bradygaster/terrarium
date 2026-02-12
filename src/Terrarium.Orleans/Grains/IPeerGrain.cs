using Terrarium.Orleans.Models;

namespace Terrarium.Orleans.Grains;

/// <summary>
/// Tracks per-peer connection state — endpoint, version, and heartbeat.
/// Keyed by peer/connection ID (string).
/// </summary>
public interface IPeerGrain : IGrainWithStringKey
{
    Task RegisterAsync(string? endpoint, string? version);
    Task HeartbeatAsync();
    Task<PeerInfo> GetInfoAsync();
}
