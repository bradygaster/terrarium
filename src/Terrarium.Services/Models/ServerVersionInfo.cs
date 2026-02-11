namespace Terrarium.Services.Models;

public sealed class ServerVersionInfo
{
    public string LatestVersion { get; init; } = string.Empty;
    public string MOTD { get; init; } = string.Empty;
}
