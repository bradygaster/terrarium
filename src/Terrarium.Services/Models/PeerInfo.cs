namespace Terrarium.Services.Models;

public sealed class PeerInfo
{
    public required string IPAddress { get; init; }
    public required string Version { get; init; }
    public string? Channel { get; init; }
    public DateTime? LastContact { get; init; }
}
