using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Terrarium.Server.Middleware;

namespace Terrarium.Server;

/// <summary>
/// Return codes for species registration.
/// Ported from Server/Website/App_Code/Species/AddSpecies.asmx.cs.
/// </summary>
public enum SpeciesServiceStatus
{
    Success,
    AlreadyExists,
    ServerDown,
    VersionIncompatible,
    FiveMinuteThrottle,
    TwentyFourHourThrottle,
    PoliCheckSpeciesNameFailure,
    PoliCheckAuthorNameFailure,
    PoliCheckEmailFailure
}

/// <summary>
/// Request body for POST /api/species/register.
/// </summary>
public sealed class RegisterSpeciesRequest
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AssemblyFullName { get; set; } = string.Empty;
    public byte[]? AssemblyCode { get; set; }
}

/// <summary>
/// Response from POST /api/species/register.
/// </summary>
public sealed class RegisterSpeciesResponse
{
    public SpeciesServiceStatus Status { get; set; }
}

/// <summary>
/// A species record returned from list/get queries.
/// </summary>
public sealed class SpeciesInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public DateTime DateAdded { get; set; }
    public string AssemblyFullName { get; set; } = string.Empty;
    public byte Extinct { get; set; }
    public DateTime? LastReintroduction { get; set; }
    public Guid? ReintroductionNode { get; set; }
    public string Version { get; set; } = string.Empty;
    public bool BlackListed { get; set; }
}

/// <summary>
/// Request body for POST /api/species/reintroduce.
/// </summary>
public sealed class ReintroduceSpeciesRequest
{
    public string Name { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public Guid PeerGuid { get; set; }
}

/// <summary>
/// Response from POST /api/species/reintroduce.
/// </summary>
public sealed class ReintroduceSpeciesResponse
{
    public bool Success { get; set; }
    public byte[]? AssemblyCode { get; set; }
}

/// <summary>
/// Minimal API endpoints for the Species service.
/// Ported from Server/Website/App_Code/Species/AddSpecies.asmx.cs.
/// </summary>
public static class SpeciesEndpoints
{
    public static RouteGroupBuilder MapSpeciesEndpoints(this RouteGroupBuilder group)
    {
        group.WithTags("Species");

        group.MapPost("/register", async (
            RegisterSpeciesRequest request,
            HttpContext httpContext,
            IOptions<ServerSettings> settings,
            ThrottleService throttle,
            ILogger<Program> logger) =>
        {
            if (string.IsNullOrEmpty(request.Name) ||
                string.IsNullOrEmpty(request.Version) ||
                string.IsNullOrEmpty(request.Type) ||
                string.IsNullOrEmpty(request.Author) ||
                string.IsNullOrEmpty(request.Email) ||
                string.IsNullOrEmpty(request.AssemblyFullName) ||
                request.AssemblyCode is null or { Length: 0 })
            {
                return Results.Ok(new RegisterSpeciesResponse { Status = SpeciesServiceStatus.VersionIncompatible });
            }

            var version = new Version(request.Version).ToString(3);
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            // Check 5-minute throttle
            if (throttle.IsThrottled(ipAddress, "AddSpecies5MinuteThrottle"))
            {
                return Results.Ok(new RegisterSpeciesResponse { Status = SpeciesServiceStatus.FiveMinuteThrottle });
            }

            // Check 24-hour throttle
            if (throttle.IsThrottled(ipAddress, "AddSpecies24HourThrottle"))
            {
                return Results.Ok(new RegisterSpeciesResponse { Status = SpeciesServiceStatus.TwentyFourHourThrottle });
            }

            try
            {
                using var connection = new SqlConnection(settings.Value.SpeciesDsn);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@Name", request.Name, DbType.String, size: 255);
                parameters.Add("@Version", version, DbType.String, size: 255);
                parameters.Add("@Type", request.Type, DbType.String, size: 50);
                parameters.Add("@Author", request.Author, DbType.String, size: 255);
                parameters.Add("@AuthorEmail", request.Email, DbType.String, size: 255);
                parameters.Add("@Extinct", (byte)0, DbType.Byte);
                parameters.Add("@DateAdded", DateTime.UtcNow, DbType.DateTime);
                parameters.Add("@AssemblyFullName", request.AssemblyFullName, DbType.String);
                parameters.Add("@BlackListed", false, DbType.Boolean);

                try
                {
                    await connection.ExecuteAsync(
                        "TerrariumInsertSpecies",
                        parameters,
                        commandType: CommandType.StoredProcedure);
                }
                catch (SqlException ex) when (ex.Number == 2627)
                {
                    return Results.Ok(new RegisterSpeciesResponse { Status = SpeciesServiceStatus.AlreadyExists });
                }

                // Save assembly to disk
                var assemblyPath = settings.Value.AssemblyPath;
                if (!string.IsNullOrEmpty(assemblyPath) && request.AssemblyCode != null)
                {
                    try
                    {
                        Directory.CreateDirectory(assemblyPath);
                        var shortName = request.AssemblyFullName.Split(',')[0].ToLowerInvariant();
                        var fileName = Path.Combine(assemblyPath, $"{shortName}.dll");
                        await File.WriteAllBytesAsync(fileName, request.AssemblyCode);
                        logger.LogInformation("Saved species assembly to {FileName}", fileName);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to save assembly to disk for {Name}", request.Name);
                        // Don't fail the registration if disk save fails
                    }
                }

                // Apply throttles after successful insert
                var introductionWait = settings.Value.IntroductionWait;
                throttle.AddThrottle(ipAddress, "AddSpecies5MinuteThrottle", 1, TimeSpan.FromMinutes(introductionWait));

                var dailyLimit = settings.Value.IntroductionDailyLimit;
                throttle.AddThrottle(ipAddress, "AddSpecies24HourThrottle", dailyLimit, TimeSpan.FromHours(24));

                return Results.Ok(new RegisterSpeciesResponse { Status = SpeciesServiceStatus.Success });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Species: Register failed");
                return Results.Ok(new RegisterSpeciesResponse { Status = SpeciesServiceStatus.ServerDown });
            }
        })
        .WithName("RegisterSpecies")
        .Produces<RegisterSpeciesResponse>();

        group.MapGet("/list", async (
            string version,
            string? filter,
            IOptions<ServerSettings> settings,
            ILogger<Program> logger) =>
        {
            if (string.IsNullOrEmpty(version))
            {
                return Results.Ok(Array.Empty<SpeciesInfo>());
            }

            var normalizedVersion = new Version(version).ToString(3);
            var sprocName = filter == "All"
                ? "TerrariumGrabAllSpecies"
                : "TerrariumGrabAllRecentSpecies";

            try
            {
                using var connection = new SqlConnection(settings.Value.SpeciesDsn);
                await connection.OpenAsync();

                var species = await connection.QueryAsync<SpeciesInfo>(
                    sprocName,
                    new { Version = normalizedVersion },
                    commandType: CommandType.StoredProcedure);

                return Results.Ok(species);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Species: List failed");
                return Results.Ok(Array.Empty<SpeciesInfo>());
            }
        })
        .WithName("ListSpecies")
        .Produces<IEnumerable<SpeciesInfo>>();

        group.MapGet("/extinct", async (
            string version,
            string? filter,
            IOptions<ServerSettings> settings,
            ILogger<Program> logger) =>
        {
            if (string.IsNullOrEmpty(version))
            {
                return Results.Ok(Array.Empty<SpeciesInfo>());
            }

            var normalizedVersion = new Version(version).ToString(3);
            var sprocName = filter == "All"
                ? "TerrariumGrabExtinctSpecies"
                : "TerrariumGrabExtinctRecentSpecies";

            try
            {
                using var connection = new SqlConnection(settings.Value.SpeciesDsn);
                await connection.OpenAsync();

                var species = await connection.QueryAsync<SpeciesInfo>(
                    sprocName,
                    new { Version = normalizedVersion },
                    commandType: CommandType.StoredProcedure);

                return Results.Ok(species);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Species: GetExtinct failed");
                return Results.Ok(Array.Empty<SpeciesInfo>());
            }
        })
        .WithName("ListExtinctSpecies")
        .Produces<IEnumerable<SpeciesInfo>>();

        group.MapGet("/blacklisted", async (
            IOptions<ServerSettings> settings,
            ILogger<Program> logger) =>
        {
            try
            {
                using var connection = new SqlConnection(settings.Value.SpeciesDsn);
                await connection.OpenAsync();

                var blacklisted = await connection.QueryAsync<string>(
                    "SELECT AssemblyFullName FROM Species WHERE BlackListed = 1");

                return Results.Ok(blacklisted);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Species: GetBlacklisted failed");
                return Results.Ok(Array.Empty<string>());
            }
        })
        .WithName("GetBlacklistedSpecies")
        .Produces<IEnumerable<string>>();

        group.MapGet("/{name}/assembly", async (
            string name,
            string version,
            IOptions<ServerSettings> settings,
            ILogger<Program> logger) =>
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(version))
            {
                return Results.BadRequest("Name and version are required");
            }

            var normalizedVersion = new Version(version).ToString(3);

            try
            {
                using var connection = new SqlConnection(settings.Value.SpeciesDsn);
                await connection.OpenAsync();

                // First check if the species exists and is not blacklisted
                var species = await connection.QuerySingleOrDefaultAsync<SpeciesInfo>(
                    "SELECT * FROM Species WHERE Name = @Name AND Version = @Version",
                    new { Name = name, Version = normalizedVersion });

                if (species == null)
                {
                    return Results.NotFound();
                }

                if (species.BlackListed)
                {
                    return Results.BadRequest("Species is blacklisted");
                }

                // Load assembly from disk
                var assemblyPath = settings.Value.AssemblyPath;
                if (string.IsNullOrEmpty(assemblyPath))
                {
                    logger.LogWarning("AssemblyPath not configured in ServerSettings");
                    return Results.Problem("Server configuration error");
                }

                var shortName = species.AssemblyFullName.Split(',')[0].ToLowerInvariant();
                var fileName = Path.Combine(assemblyPath, $"{shortName}.dll");

                if (!File.Exists(fileName))
                {
                    logger.LogWarning("Assembly file not found: {FileName}", fileName);
                    return Results.NotFound("Assembly file not found");
                }

                var assemblyBytes = await File.ReadAllBytesAsync(fileName);
                return Results.Bytes(assemblyBytes, "application/octet-stream");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Species: GetAssembly failed for {Name}", name);
                return Results.Problem("Failed to retrieve species assembly");
            }
        })
        .WithName("GetSpeciesAssembly")
        .Produces<byte[]>();

        group.MapPost("/reintroduce", async (
            ReintroduceSpeciesRequest request,
            IOptions<ServerSettings> settings,
            ILogger<Program> logger) =>
        {
            if (string.IsNullOrEmpty(request.Name) ||
                string.IsNullOrEmpty(request.Version) ||
                request.PeerGuid == Guid.Empty)
            {
                return Results.Ok(new ReintroduceSpeciesResponse { Success = false });
            }

            var version = new Version(request.Version).ToString(3);

            try
            {
                using var connection = new SqlConnection(settings.Value.SpeciesDsn);
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                // Check if the species is extinct
                var extinctCount = await connection.ExecuteScalarAsync<int?>(
                    "TerrariumCheckSpeciesExtinct",
                    new { Name = request.Name },
                    transaction,
                    commandType: CommandType.StoredProcedure);

                if (extinctCount is null or 0)
                {
                    await transaction.RollbackAsync();
                    return Results.Ok(new ReintroduceSpeciesResponse { Success = false });
                }

                // Mark species as reintroduced
                await connection.ExecuteAsync(
                    "TerrariumReintroduceSpecies",
                    new
                    {
                        Name = request.Name,
                        ReintroductionNode = request.PeerGuid,
                        LastReintroduction = DateTime.UtcNow
                    },
                    transaction,
                    commandType: CommandType.StoredProcedure);

                await transaction.CommitAsync();
                return Results.Ok(new ReintroduceSpeciesResponse { Success = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Species: Reintroduce failed for {Name}", request.Name);
                return Results.Ok(new ReintroduceSpeciesResponse { Success = false });
            }
        })
        .WithName("ReintroduceSpecies")
        .Produces<ReintroduceSpeciesResponse>();

        return group;
    }
}
