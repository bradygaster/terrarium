namespace Terrarium.Services.Models;

public sealed class PeerRegistrationResult
{
    public required RegisterPeerResult Status { get; init; }
    public int PeerCount { get; init; }
    public IReadOnlyList<PeerInfo> Peers { get; init; } = [];
}
