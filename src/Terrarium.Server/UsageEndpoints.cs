using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Terrarium.Server.Middleware;

namespace Terrarium.Server;

/// <summary>
/// Request body for POST /api/usage/heartbeat.
/// </summary>
public sealed class HeartbeatRequest
{
    public Guid Guid { get; set; }
    public int CurrentTick { get; set; }
}

/// <summary>
/// Response from POST /api/usage/heartbeat.
/// </summary>
public sealed class HeartbeatResponse
{
    public bool Success { get; set; }
}

/// <summary>
/// Response from GET /api/usage/daily-stats.
/// </summary>
public sealed class DailyStatsResponse
{
    public int ActivePeers { get; set; }
    public int SpeciesCount { get; set; }
    public int TotalPopulation { get; set; }
    public DateTime? LastRollup { get; set; }
}

/// <summary>
/// Response from GET /api/usage/version-check.
/// </summary>
public sealed class ServerVersionResponse
{
    public string LatestVersion { get; set; } = string.Empty;
    public string MOTD { get; set; } = string.Empty;
}

/// <summary>
/// Minimal API endpoints for the Usage service.
/// Ported from Server/Website/App_Code/UsageService.cs and related usage tracking.
/// Provides heartbeat, daily statistics, and version info.
/// </summary>
public static class UsageEndpoints
{
    public static RouteGroupBuilder MapUsageEndpoints(this RouteGroupBuilder group)
    {
        // Client heartbeat (peer alive signal)
        group.MapPost("/heartbeat", async (
            HeartbeatRequest request,
            HttpContext httpContext,
            IOptions<ServerSettings> settings,
            ThrottleService throttle,
            ILogger<Program> logger) =>
        {
            if (request.Guid == Guid.Empty)
            {
                return Results.Ok(new HeartbeatResponse { Success = false });
            }

            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Throttle heartbeats to once per minute per peer
            if (throttle.IsThrottled(ipAddress, "Heartbeat"))
            {
                return Results.Ok(new HeartbeatResponse { Success = true });
            }

            try
            {
                using var connection = new SqlConnection(settings.Value.SpeciesDsn);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@Guid", request.Guid, DbType.Guid);
                parameters.Add("@LastContact", DateTime.UtcNow, DbType.DateTime);
                parameters.Add("@LastTick", request.CurrentTick, DbType.Int32);
                parameters.Add("@ReturnCode", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await connection.ExecuteAsync(
                    "TerrariumTimeoutReport",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                throttle.AddThrottle(ipAddress, "Heartbeat", 1, TimeSpan.FromMinutes(1));

                return Results.Ok(new HeartbeatResponse { Success = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Usage: Heartbeat failed for {Guid}", request.Guid);
                return Results.Ok(new HeartbeatResponse { Success = false });
            }
        });

        // Daily active users, creature count, total ticks
        group.MapGet("/daily-stats", async (
            IOptions<ServerSettings> settings,
            ILogger<Program> logger) =>
        {
            try
            {
                using var connection = new SqlConnection(settings.Value.SpeciesDsn);
                await connection.OpenAsync();

                // Count active peers from NodeLastContact (contacted in last 24 hours)
                var activePeers = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM NodeLastContact WHERE LastContact > DATEADD(hh, -24, GETUTCDATE())");

                // Count non-extinct, non-blacklisted species
                var speciesCount = await connection.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Species WHERE Extinct = 0 AND BlackListed = 0");

                // Total population from latest rollup
                var totalPopulation = await connection.ExecuteScalarAsync<int?>(
                    @"SELECT SUM(Population) FROM DailyPopulation
                      WHERE SampleDateTime = (SELECT MAX(SampleDateTime) FROM DailyPopulation)") ?? 0;

                // Last rollup time
                var lastRollup = await connection.ExecuteScalarAsync<DateTime?>(
                    "SELECT MAX(SampleDateTime) FROM DailyPopulation");

                return Results.Ok(new DailyStatsResponse
                {
                    ActivePeers = activePeers,
                    SpeciesCount = speciesCount,
                    TotalPopulation = totalPopulation,
                    LastRollup = lastRollup
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Usage: DailyStats failed");
                return Results.Ok(new DailyStatsResponse());
            }
        });

        // Server version info for client compatibility
        group.MapGet("/version-check", (IOptions<ServerSettings> settings) =>
        {
            return Results.Ok(new ServerVersionResponse
            {
                LatestVersion = settings.Value.LatestVersion,
                MOTD = settings.Value.MOTD
            });
        });

        return group;
    }
}
