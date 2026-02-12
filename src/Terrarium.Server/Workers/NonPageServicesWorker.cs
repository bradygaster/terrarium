using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace Terrarium.Server.Workers;

/// <summary>
/// Background worker that performs periodic maintenance tasks.
/// Ported from Server/Website/App_Code/Code/NonPageServices.cs.
///
/// Tasks:
/// - Stale peer cleanup: removes peers that haven't sent heartbeat in 5 minutes
/// - Population snapshot: runs TerrariumAggregate to roll up History into DailyPopulation
/// - Blacklist refresh: periodically reloads the species blacklist
/// </summary>
public sealed class NonPageServicesWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NonPageServicesWorker> _logger;

    // Cached blacklist, refreshed periodically
    private volatile HashSet<string> _blacklistedSpecies = [];

    public NonPageServicesWorker(
        IServiceProvider serviceProvider,
        ILogger<NonPageServicesWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    /// <summary>
    /// Returns the current set of blacklisted assembly names.
    /// </summary>
    public IReadOnlySet<string> BlacklistedSpecies => _blacklistedSpecies;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("NonPageServicesWorker starting");

        // Initial blacklist load (skip if no DB configured)
        if (HasDatabaseConnection())
        {
            await RefreshBlacklistAsync(stoppingToken);
        }
        else
        {
            _logger.LogInformation("NonPageServicesWorker: no database connection configured, running in local-only mode");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var settings = _serviceProvider.GetRequiredService<IOptions<ServerSettings>>().Value;
                var interval = TimeSpan.FromMilliseconds(settings.MillisecondsToRollupData);

                _logger.LogDebug("NonPageServicesWorker heartbeat — next cycle in {Interval}", interval);

                await Task.Delay(interval, stoppingToken);

                if (!HasDatabaseConnection()) continue;

                await CleanupStalePeersAsync(stoppingToken);
                await RunPopulationSnapshotAsync(stoppingToken);
                await RefreshBlacklistAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "NonPageServicesWorker: error during maintenance cycle");

                // Wait a bit before retrying to avoid tight error loops
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        _logger.LogInformation("NonPageServicesWorker stopped");
    }

    /// <summary>
    /// Returns true if a database connection string is configured.
    /// </summary>
    private bool HasDatabaseConnection()
    {
        try
        {
            var settings = _serviceProvider.GetRequiredService<IOptions<ServerSettings>>().Value;
            return !string.IsNullOrEmpty(settings.SpeciesDsn);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Remove peers whose lease has expired (no heartbeat in 5 minutes).
    /// Mirrors the peer expiration logic from TerrariumAggregate.
    /// </summary>
    private async Task CleanupStalePeersAsync(CancellationToken ct)
    {
        try
        {
            var settings = _serviceProvider.GetRequiredService<IOptions<ServerSettings>>().Value;
            using var connection = new SqlConnection(settings.SpeciesDsn);
            await connection.OpenAsync(ct);

            // Move expired peers to ShutdownPeers and delete from Peers
            var removed = await connection.ExecuteAsync(
                @"INSERT INTO ShutdownPeers (Guid, Channel, IPAddress, FirstContact, LastContact, Version, UnRegister)
                  SELECT Guid, Channel, IPAddress, FirstContact, GETUTCDATE(), Version, 0
                  FROM Peers WHERE Lease < GETUTCDATE();

                  DELETE FROM Peers WHERE Lease < GETUTCDATE();");

            if (removed > 0)
            {
                _logger.LogInformation("NonPageServicesWorker: cleaned up {Count} stale peers", removed);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NonPageServicesWorker: stale peer cleanup failed");
        }
    }

    /// <summary>
    /// Roll up History data into DailyPopulation snapshots via TerrariumAggregate.
    /// This is the core data aggregation that powers the charts.
    /// </summary>
    private async Task RunPopulationSnapshotAsync(CancellationToken ct)
    {
        try
        {
            var settings = _serviceProvider.GetRequiredService<IOptions<ServerSettings>>().Value;
            using var connection = new SqlConnection(settings.SpeciesDsn);
            await connection.OpenAsync(ct);

            var parameters = new DynamicParameters();
            parameters.Add("@Expiration_Error", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@Rollup_Error", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@Timeout_Add_Error", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@Timeout_Delete_Error", dbType: DbType.Int32, direction: ParameterDirection.Output);
            parameters.Add("@Extinction_Error", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "TerrariumAggregate",
                parameters,
                commandTimeout: 120,
                commandType: CommandType.StoredProcedure);

            var expirationError = parameters.Get<int>("@Expiration_Error");
            var rollupError = parameters.Get<int>("@Rollup_Error");
            var timeoutAddError = parameters.Get<int>("@Timeout_Add_Error");
            var timeoutDeleteError = parameters.Get<int>("@Timeout_Delete_Error");
            var extinctionError = parameters.Get<int>("@Extinction_Error");

            if (expirationError != 0 || rollupError != 0 || timeoutAddError != 0 ||
                timeoutDeleteError != 0 || extinctionError != 0)
            {
                _logger.LogWarning(
                    "NonPageServicesWorker: TerrariumAggregate completed with errors — " +
                    "Expiration={Expiration}, Rollup={Rollup}, TimeoutAdd={TimeoutAdd}, " +
                    "TimeoutDelete={TimeoutDelete}, Extinction={Extinction}",
                    expirationError, rollupError, timeoutAddError, timeoutDeleteError, extinctionError);
            }
            else
            {
                _logger.LogInformation("NonPageServicesWorker: population snapshot completed successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NonPageServicesWorker: population snapshot failed");
        }
    }

    /// <summary>
    /// Reload the species blacklist from the database.
    /// </summary>
    private async Task RefreshBlacklistAsync(CancellationToken ct)
    {
        try
        {
            var settings = _serviceProvider.GetRequiredService<IOptions<ServerSettings>>().Value;
            using var connection = new SqlConnection(settings.SpeciesDsn);
            await connection.OpenAsync(ct);

            var names = await connection.QueryAsync<string>(
                "SELECT AssemblyFullName FROM Species WHERE BlackListed = 1");

            _blacklistedSpecies = new HashSet<string>(names, StringComparer.OrdinalIgnoreCase);

            _logger.LogDebug("NonPageServicesWorker: blacklist refreshed, {Count} entries", _blacklistedSpecies.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NonPageServicesWorker: blacklist refresh failed");
        }
    }
}
