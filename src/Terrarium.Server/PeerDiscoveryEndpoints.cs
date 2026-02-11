using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Terrarium.Server;

/// <summary>
/// Result codes returned by the peer registration endpoint.
/// Ported from Server/Website/App_Code/Discovery/DiscoveryDB.asmx.cs.
/// </summary>
public enum RegisterPeerResult
{
    Success,
    Failure,
    GlobalFailure
}

/// <summary>
/// A peer entry returned from the discovery stored procedure.
/// </summary>
public sealed class PeerInfo
{
    public string IPAddress { get; set; } = string.Empty;
    public DateTime Lease { get; set; }
}

/// <summary>
/// Request body for POST /api/discovery/register.
/// </summary>
public sealed class RegisterPeerRequest
{
    public string Version { get; set; } = string.Empty;
    public string Channel { get; set; } = string.Empty;
    public Guid Guid { get; set; }
}

/// <summary>
/// Response from POST /api/discovery/register.
/// </summary>
public sealed class RegisterPeerResponse
{
    public RegisterPeerResult Result { get; set; }
    public int PeerCount { get; set; }
    public List<PeerInfo> Peers { get; set; } = [];
}

/// <summary>
/// Request body for POST /api/discovery/register-user.
/// </summary>
public sealed class RegisterUserRequest
{
    public string Email { get; set; } = string.Empty;
}

/// <summary>
/// Response from GET /api/discovery/peers.
/// </summary>
public sealed class PeerCountResponse
{
    public int Count { get; set; }
}

/// <summary>
/// Request for checking if a version is disabled.
/// </summary>
public sealed class VersionCheckResponse
{
    public bool Disabled { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Minimal API endpoints for the PeerDiscovery service.
/// Ported from Server/Website/App_Code/Discovery/DiscoveryDB.asmx.cs.
/// </summary>
public static class PeerDiscoveryEndpoints
{
    public static RouteGroupBuilder MapPeerDiscoveryEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/register", async (
            RegisterPeerRequest request,
            HttpContext httpContext,
            IOptions<ServerSettings> settings,
            ILogger<Program> logger) =>
        {
            if (string.IsNullOrEmpty(request.Version) || string.IsNullOrEmpty(request.Channel))
            {
                return Results.Ok(new RegisterPeerResponse { Result = RegisterPeerResult.GlobalFailure });
            }

            var fullVersion = new Version(request.Version).ToString(4);
            var version = new Version(request.Version).ToString(3);
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            try
            {
                using var connection = new SqlConnection(settings.Value.SpeciesDsn);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@Version", version, DbType.String, size: 255);
                parameters.Add("@FullVersion", fullVersion, DbType.String, size: 255);
                parameters.Add("@Channel", request.Channel, DbType.String, size: 255);
                parameters.Add("@IPAddress", ipAddress, DbType.String, size: 50);
                parameters.Add("@Guid", request.Guid, DbType.Guid);
                parameters.Add("@Disabled_Error", dbType: DbType.Boolean, direction: ParameterDirection.Output);
                parameters.Add("@PeerCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var peers = (await connection.QueryAsync<PeerInfo>(
                    "TerrariumRegisterPeerCountAndList",
                    parameters,
                    commandType: CommandType.StoredProcedure)).ToList();

                var disabled = parameters.Get<bool>("@Disabled_Error");
                var peerCount = parameters.Get<int>("@PeerCount");

                if (disabled)
                {
                    return Results.Ok(new RegisterPeerResponse { Result = RegisterPeerResult.GlobalFailure });
                }

                return Results.Ok(new RegisterPeerResponse
                {
                    Result = RegisterPeerResult.Success,
                    PeerCount = peerCount,
                    Peers = peers
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PeerDiscovery: RegisterPeer failed");
                return Results.Ok(new RegisterPeerResponse { Result = RegisterPeerResult.Failure });
            }
        });

        group.MapGet("/peers", async (
            string version,
            string channel,
            IOptions<ServerSettings> settings,
            ILogger<Program> logger) =>
        {
            if (string.IsNullOrEmpty(version) || string.IsNullOrEmpty(channel))
            {
                return Results.Ok(new PeerCountResponse { Count = 0 });
            }

            var normalizedVersion = new Version(version).ToString(3);

            try
            {
                using var connection = new SqlConnection(settings.Value.SpeciesDsn);
                await connection.OpenAsync();

                var count = await connection.ExecuteScalarAsync<int?>(
                    "TerrariumGrabNumPeers",
                    new { Version = normalizedVersion, Channel = channel },
                    commandType: CommandType.StoredProcedure);

                return Results.Ok(new PeerCountResponse { Count = count ?? 0 });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PeerDiscovery: GetNumPeers failed");
                return Results.Ok(new PeerCountResponse { Count = 0 });
            }
        });

        group.MapGet("/validate", (HttpContext httpContext) =>
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return Results.Ok(new { ipAddress });
        });

        group.MapPost("/register-user", async (
            RegisterUserRequest request,
            HttpContext httpContext,
            IOptions<ServerSettings> settings,
            ILogger<Program> logger) =>
        {
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            try
            {
                using var connection = new SqlConnection(settings.Value.SpeciesDsn);
                await connection.OpenAsync();

                await connection.ExecuteAsync(
                    "TerrariumRegisterUser",
                    new { Email = request.Email, IPAddress = ipAddress },
                    commandType: CommandType.StoredProcedure);

                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PeerDiscovery: RegisterUser failed");
                return Results.Ok(new { success = false });
            }
        });

        group.MapGet("/version-check", async (
            string version,
            IOptions<ServerSettings> settings,
            ILogger<Program> logger) =>
        {
            try
            {
                var fullVersion = new Version(version).ToString(4);
                var normalizedVersion = new Version(version).ToString(3);

                using var connection = new SqlConnection(settings.Value.SpeciesDsn);
                await connection.OpenAsync();

                var result = await connection.QueryFirstOrDefaultAsync<VersionCheckResponse>(
                    "TerrariumIsVersionDisabled",
                    new { Version = normalizedVersion, FullVersion = fullVersion },
                    commandType: CommandType.StoredProcedure);

                return Results.Ok(result ?? new VersionCheckResponse { Disabled = true, Message = string.Empty });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "PeerDiscovery: IsVersionDisabled failed");
                return Results.Ok(new VersionCheckResponse { Disabled = true, Message = string.Empty });
            }
        });

        return group;
    }
}
