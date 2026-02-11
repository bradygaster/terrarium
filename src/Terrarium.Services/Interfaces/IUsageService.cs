using Terrarium.Services.Models;

namespace Terrarium.Services.Interfaces;

public interface IUsageService
{
    Task<HeartbeatResult> SendHeartbeatAsync(Guid peerGuid, int currentTick,
        CancellationToken cancellationToken = default);

    Task<DailyStats> GetDailyStatsAsync(CancellationToken cancellationToken = default);

    Task<ServerVersionInfo> GetServerVersionAsync(CancellationToken cancellationToken = default);
}
