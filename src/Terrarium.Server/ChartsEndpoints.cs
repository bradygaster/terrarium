using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Terrarium.Server;

/// <summary>
/// A population history data point for time-series charts.
/// </summary>
public sealed class PopulationHistoryEntry
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
/// A species distribution entry for pie charts.
/// </summary>
public sealed class SpeciesDistributionEntry
{
    public string SpeciesName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public int Population { get; set; }
}

/// <summary>
/// A top creature entry for leaderboard display.
/// </summary>
public sealed class TopCreatureEntry
{
    public string Name { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Population { get; set; }
    public int KilledCount { get; set; }
}

/// <summary>
/// Minimal API endpoints for the Charts service.
/// Ported from Server/Website/App_Code/Charts/ChartService.asmx.cs and ChartBuilder.cs.
/// Provides ecosystem statistics data for dashboard charts.
/// </summary>
public static class ChartsEndpoints
{
    public static RouteGroupBuilder MapChartsEndpoints(this RouteGroupBuilder group)
    {
        // Population over time by species (for time-series chart)
        group.MapGet("/population-history", async (
            string species,
            DateTime? beginDate,
            DateTime? endDate,
            IOptions<ServerSettings> settings,
            ILogger<Program> logger) =>
        {
            try
            {
                using var connection = new SqlConnection(settings.Value.SpeciesDsn);
                await connection.OpenAsync();

                // If date range provided, use range query; otherwise grab latest
                if (beginDate.HasValue && endDate.HasValue)
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@SpeciesName", species, DbType.String, size: 50);
                    parameters.Add("@BeginDate", beginDate.Value, DbType.DateTime);
                    parameters.Add("@EndDate", endDate.Value, DbType.DateTime);

                    var data = await connection.QueryAsync<PopulationHistoryEntry>(
                        "TerrariumGrabSpeciesDataInDateRange",
                        parameters,
                        commandType: CommandType.StoredProcedure);

                    return Results.Ok(data);
                }
                else
                {
                    var data = await connection.QueryAsync<PopulationHistoryEntry>(
                        "TerrariumGrabLatestSpeciesData",
                        new { SpeciesName = species },
                        commandType: CommandType.StoredProcedure);

                    return Results.Ok(data);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Charts: PopulationHistory failed for {Species}", species);
                return Results.Ok(Array.Empty<PopulationHistoryEntry>());
            }
        });

        // Current species distribution (for pie chart)
        group.MapGet("/species-distribution", async (
            IOptions<ServerSettings> settings,
            ILogger<Program> logger) =>
        {
            try
            {
                using var connection = new SqlConnection(settings.Value.SpeciesDsn);
                await connection.OpenAsync();

                var data = await connection.QueryAsync<SpeciesDistributionEntry>(
                    "TerrariumGrabSpeciesInfo",
                    commandType: CommandType.StoredProcedure);

                return Results.Ok(data);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Charts: SpeciesDistribution failed");
                return Results.Ok(Array.Empty<SpeciesDistributionEntry>());
            }
        });

        // Top N creatures by population (for leaderboard)
        group.MapGet("/top-creatures", async (
            string version,
            string? type,
            int? count,
            IOptions<ServerSettings> settings,
            ILogger<Program> logger) =>
        {
            var num = count ?? 10;
            var speciesType = type ?? "Animal";

            try
            {
                var normalizedVersion = new Version(version).ToString(3);

                using var connection = new SqlConnection(settings.Value.SpeciesDsn);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@Count", num, DbType.Int32);
                parameters.Add("@Version", normalizedVersion, DbType.String, size: 25);
                parameters.Add("@SpeciesType", speciesType, DbType.String, size: 50);

                var animals = await connection.QueryAsync<TopCreatureEntry>(
                    "TerrariumTopAnimals",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                return Results.Ok(animals);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Charts: TopCreatures failed");
                return Results.Ok(Array.Empty<TopCreatureEntry>());
            }
        });

        return group;
    }
}
