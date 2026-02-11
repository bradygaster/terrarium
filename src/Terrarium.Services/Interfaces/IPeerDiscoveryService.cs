using Terrarium.Services.Models;

namespace Terrarium.Services.Interfaces;

public interface IPeerDiscoveryService
{
    Task<bool> RegisterUserAsync(string email, CancellationToken cancellationToken = default);
    Task<int> GetNumPeersAsync(string version, string channel, CancellationToken cancellationToken = default);
    Task<string> ValidatePeerAsync(CancellationToken cancellationToken = default);
    Task<PeerRegistrationResult> RegisterPeerAsync(string version, string channel, Guid peerGuid, CancellationToken cancellationToken = default);
    Task<VersionCheckResult> IsVersionDisabledAsync(string version, CancellationToken cancellationToken = default);
}
