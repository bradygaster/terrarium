using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Terrarium.Server;

/// <summary>
/// Request body for POST /api/watson/report.
/// Ported from Server/Website/App_Code/Watson/Watson.asmx.cs.
/// </summary>
public sealed class WatsonReportRequest
{
    public string LogType { get; set; } = string.Empty;
    public string OSVersion { get; set; } = string.Empty;
    public string GameVersion { get; set; } = string.Empty;
    public string CLRVersion { get; set; } = string.Empty;
    public string ErrorLog { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string UserComment { get; set; } = string.Empty;
}

/// <summary>
/// Request body for POST /api/bugs/report.
/// Ported from Server/Website/App_Code/BugService.cs (stub in legacy).
/// </summary>
public sealed class BugReportRequest
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}

/// <summary>
/// Minimal API endpoints for the Watson error reporting and Bug reporting services.
/// Watson: ported from Server/Website/App_Code/Watson/Watson.asmx.cs.
/// Bug: ported from Server/Website/App_Code/BugService.cs (was a stub/TODO in legacy).
/// </summary>
public static class WatsonEndpoints
{
    public static RouteGroupBuilder MapWatsonEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/report", async (
            WatsonReportRequest request,
            HttpContext httpContext,
            IOptions<ServerSettings> settings,
            ILogger<Program> logger) =>
        {
            try
            {
                var machineName = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                using var connection = new SqlConnection(settings.Value.SpeciesDsn);
                await connection.OpenAsync();

                await connection.ExecuteAsync(
                    "TerrariumInsertWatson",
                    new
                    {
                        request.LogType,
                        MachineName = machineName,
                        request.OSVersion,
                        request.GameVersion,
                        request.CLRVersion,
                        request.ErrorLog,
                        request.UserComment,
                        request.UserEmail
                    },
                    commandType: CommandType.StoredProcedure);

                return Results.Ok(new { success = true });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Watson: ReportError failed");
                return Results.Ok(new { success = false });
            }
        });

        return group;
    }

    public static RouteGroupBuilder MapBugEndpoints(this RouteGroupBuilder group)
    {
        group.MapPost("/report", (
            BugReportRequest request,
            ILogger<Program> logger) =>
        {
            // Legacy BugService was a stub (TODO in code). Log the report for now.
            logger.LogInformation("Bug report received: {Title} from {Alias}", request.Title, request.Alias);
            return Results.Ok(new { success = true });
        });

        return group;
    }
}
