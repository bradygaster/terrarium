namespace Terrarium.Services.Models;

public sealed class VersionCheckResult
{
    public required bool IsDisabled { get; init; }
    public string? ErrorMessage { get; init; }
}
