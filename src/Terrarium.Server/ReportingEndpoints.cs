using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Terrarium.Server.Middleware;

namespace Terrarium.Server;

/// <summary>
/// Return codes for the reporting service.
/// Ported from Server/Website/App_Code/Reporting/ReportPopulation.asmx.cs.
/// </summary>
public enum ReportingReturnCode
{
    Success = 0,
    AlreadyExists = 1,
    ServerDown = 2,
    NodeTimedOut = 3,
    NodeCorrupted = 4,
    OrganismBlacklisted = 5
}

/// <summary>
/// A single row of population data reported by a client.
/// </summary>
public sealed class PopulationHistoryRow
{
    public Guid Guid { get; set; }
    public int TickNumber { get; set; }
    public string SpeciesName { get; set; } = string.Empty;
    public DateTime ClientTime { get; set; }
    public int CorrectTime { get; set; }
    public int Population { get; set; }
    public int BirthCount { get; set; }
    public int TeleportedToCount { get; set; }
    public int StarvedCount { get; set; }
    public int KilledCount { get; set; }
    public int TeleportedFromCount { get; set; }
    public int ErrorCount { get; set; }
    public int TimeoutCount { get; set; }
    public int SickCount { get; set; }
    public int OldAgeCount { get; set; }
    public int SecurityViolationCount { get; set; }
}

/// <summary>
/// Request body for POST /api/reporting/population.
/// </summary>
public sealed class ReportPopulationRequest
{
    public Guid Guid { get; set; }
    public int CurrentTick { get; set; }
    public List<PopulationHistoryRow> History { get; set; } = [];
}

/// <summary>
/// Response from POST /api/reporting/population.
/// </summary>
public sealed class ReportPopulationResponse
{
    public ReportingReturnCode ReturnCode { get; set; }
}

/// <summary>
/// A species data point from the DailyPopulation table.
/// </summary>
public sealed class SpeciesPopulationData
{
    public DateTime SampleDateTime { get; set; }
    public string SpeciesName { get; set; } = string.Empty;
    public int Population { get; set; }
    public int BirthCount { get; set; }
    public int StarvedCount { get; set; }
    public int KilledCount { get; set; }
    public int ErrorCount { get; set; }
    public int TimeoutCount { get; set; }
    public int SickCount { get; set; }
    public int OldAgeCount { get; set; }
    public int SecurityViolationCount { get; set; }
}

/// <summary>
/// A species info record joined with population data.
/// Returned by the TerrariumGrabSpeciesInfo stored procedure.
/// </summary>
public sealed class SpeciesWithPopulation
{
    public string SpeciesName { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorEmail { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public int Population { get; set; }
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// A top-animal entry returned by the TerrariumTopAnimals stored procedure.
/// </summary>
public sealed class TopAnimalEntry
{
    public string Name { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public int Population { get; set; }
}

/// <summary>
/// Minimal API endpoints for the Reporting and Charts services.
/// Reporting: ported from Server/Website/App_Code/Reporting/ReportPopulation.asmx.cs.
/// Charts: ported from Server/Website/App_Code/Charts/ChartService.asmx.cs and ChartBuilder.cs.
/// </summary>
public static class ReportingEndpoints
{
    public static RouteGroupBuilder MapReportingEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/population", async (
            ReportPopulationRequest request,
            HttpContext httpContext,
            IOptions<ServerSettings> settings,
            ThrottleService throttle,
            ILogger<Program> logger) =>
        {
            try
            {
                if (request.Guid == Guid.Empty || request.History.Count == 0)
                {
                    return Results.Ok(new ReportPopulationResponse { ReturnCode = ReportingReturnCode.Success });
                }

                var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                // Check for blacklisted species
                using var checkConnection = new SqlConnection(settings.Value.SpeciesDsn);
                await checkConnection.OpenAsync();

                foreach (var row in request.History)
                {
                    var blacklisted = await checkConnection.ExecuteScalarAsync<int?>(
                        "TerrariumCheckSpeciesBlackList",
                        new { Name = row.SpeciesName },
                        commandType: CommandType.StoredProcedure);

                    if (blacklisted is 1)
                    {
                        return Results.Ok(new ReportPopulationResponse { ReturnCode = ReportingReturnCode.OrganismBlacklisted });
                    }
                }

                // Check throttles
                if (throttle.IsThrottled(ipAddress, "ReportPopulation3Mins"))
                {
                    return Results.Ok(new ReportPopulationResponse { ReturnCode = ReportingReturnCode.Success });
                }

                if (throttle.IsThrottled(ipAddress, "ReportPopulation12Hour"))
                {
                    return Results.Ok(new ReportPopulationResponse { ReturnCode = ReportingReturnCode.Success });
                }

                throttle.AddThrottle(ipAddress, "ReportPopulation3Mins", 1, TimeSpan.FromMinutes(3));

                var contactTime = DateTime.UtcNow;

                using var connection = new SqlConnection(settings.Value.SpeciesDsn);
                await connection.OpenAsync();
                using var transaction = await connection.BeginTransactionAsync();

                // Call TerrariumTimeoutReport
                var timeoutParams = new DynamicParameters();
                timeoutParams.Add("@Guid", request.Guid, DbType.Guid);
                timeoutParams.Add("@LastContact", contactTime, DbType.DateTime);
                timeoutParams.Add("@LastTick", request.CurrentTick, DbType.Int32);
                timeoutParams.Add("@ReturnCode", dbType: DbType.Int32, direction: ParameterDirection.Output);

                await connection.ExecuteAsync(
                    "TerrariumTimeoutReport",
                    timeoutParams,
                    (System.Data.Common.DbTransaction)transaction,
                    commandType: CommandType.StoredProcedure);

                var returnCode = timeoutParams.Get<int>("@ReturnCode");
                if (returnCode != 0)
                {
                    return returnCode switch
                    {
                        1 => Results.Ok(new ReportPopulationResponse { ReturnCode = ReportingReturnCode.NodeTimedOut }),
                        2 => Results.Ok(new ReportPopulationResponse { ReturnCode = ReportingReturnCode.NodeCorrupted }),
                        _ => Results.Ok(new ReportPopulationResponse { ReturnCode = ReportingReturnCode.ServerDown })
                    };
                }

                // Validate and insert history rows
                if (request.History.Count > 600)
                {
                    await transaction.RollbackAsync();
                    return Results.Ok(new ReportPopulationResponse { ReturnCode = ReportingReturnCode.Success });
                }

                var totalPopulation = 0;
                var validRecord = true;
                var foundBlacklisted = false;

                foreach (var row in request.History)
                {
                    if (row.TickNumber != request.CurrentTick)
                        continue;

                    totalPopulation += row.Population;
                    if (totalPopulation > 600 || row.Population > 340 || row.Population < 0)
                    {
                        validRecord = false;
                        break;
                    }

                    var correctTime = row.TickNumber == request.CurrentTick ? row.CorrectTime : 0;

                    var historyParams = new DynamicParameters();
                    historyParams.Add("@Guid", row.Guid, DbType.Guid);
                    historyParams.Add("@SpeciesName", row.SpeciesName, DbType.String, size: 255);
                    historyParams.Add("@ContactTime", contactTime, DbType.DateTime);
                    historyParams.Add("@ClientTime", row.ClientTime, DbType.DateTime);
                    historyParams.Add("@CorrectTime", (byte)correctTime, DbType.Byte);
                    historyParams.Add("@TickNumber", row.TickNumber, DbType.Int32);
                    historyParams.Add("@Population", row.Population, DbType.Int32);
                    historyParams.Add("@BirthCount", row.BirthCount, DbType.Int32);
                    historyParams.Add("@TeleportedToCount", row.TeleportedToCount, DbType.Int32);
                    historyParams.Add("@StarvedCount", row.StarvedCount, DbType.Int32);
                    historyParams.Add("@KilledCount", row.KilledCount, DbType.Int32);
                    historyParams.Add("@TeleportedFromCount", row.TeleportedFromCount, DbType.Int32);
                    historyParams.Add("@ErrorCount", row.ErrorCount, DbType.Int32);
                    historyParams.Add("@TimeoutCount", row.TimeoutCount, DbType.Int32);
                    historyParams.Add("@SickCount", row.SickCount, DbType.Int32);
                    historyParams.Add("@OldAgeCount", row.OldAgeCount, DbType.Int32);
                    historyParams.Add("@SecurityViolationCount", row.SecurityViolationCount, DbType.Int32);
                    historyParams.Add("@BlackListed", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    await connection.ExecuteAsync(
                        "TerrariumInsertHistory",
                        historyParams,
                        (System.Data.Common.DbTransaction)transaction,
                        commandType: CommandType.StoredProcedure);

                    var bl = historyParams.Get<int?>("@BlackListed");
                    if (bl is 1)
                    {
                        foundBlacklisted = true;
                    }
                }

                if (validRecord)
                    await transaction.CommitAsync();
                else
                    await transaction.RollbackAsync();

                if (foundBlacklisted)
                    return Results.Ok(new ReportPopulationResponse { ReturnCode = ReportingReturnCode.OrganismBlacklisted });

                return Results.Ok(new ReportPopulationResponse { ReturnCode = ReportingReturnCode.Success });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Reporting: ReportPopulation failed");
                return Results.Ok(new ReportPopulationResponse { ReturnCode = ReportingReturnCode.Success });
            }
        });

        group.MapGet("/stats/species-list", async (
            IOptions<ServerSettings> settings,
            ILogger<Program> logger) =>
        {
            try
            {
                using var connection = new SqlConnection(settings.Value.SpeciesDsn);
                await connection.OpenAsync();

                var species = await connection.QueryAsync<SpeciesWithPopulation>(
                    "TerrariumGrabSpeciesInfo",
                    commandType: CommandType.StoredProcedure);

                return Results.Ok(species);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Reporting: GetSpeciesList failed");
                return Results.Ok(Array.Empty<SpeciesWithPopulation>());
            }
        });

        group.MapGet("/stats/latest", async (
            string species,
            IOptions<ServerSettings> settings,
            ILogger<Program> logger) =>
        {
            try
            {
                using var connection = new SqlConnection(settings.Value.SpeciesDsn);
                await connection.OpenAsync();

                var data = await connection.QueryAsync<SpeciesPopulationData>(
                    "TerrariumGrabLatestSpeciesData",
                    new { SpeciesName = species },
                    commandType: CommandType.StoredProcedure);

                return Results.Ok(data);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Reporting: GrabLatestSpeciesData failed for {Species}", species);
                return Results.Ok(Array.Empty<SpeciesPopulationData>());
            }
        });

        group.MapGet("/stats/top-animals", async (
            string version,
            string type,
            int? count,
            IOptions<ServerSettings> settings,
            ILogger<Program> logger) =>
        {
            var num = count ?? 3;

            try
            {
                var normalizedVersion = new Version(version).ToString(3);

                using var connection = new SqlConnection(settings.Value.SpeciesDsn);
                await connection.OpenAsync();

                var animals = await connection.QueryAsync<TopAnimalEntry>(
                    "TerrariumTopAnimals",
                    new { Count = num, Version = normalizedVersion, SpeciesType = type },
                    commandType: CommandType.StoredProcedure);

                return Results.Ok(animals);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Reporting: GetTopAnimals failed");
                return Results.Ok(Array.Empty<TopAnimalEntry>());
            }
        });

        return group;
    }
}
