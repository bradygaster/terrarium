using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Terrarium.Server.HealthChecks;

/// <summary>
/// Health check for assembly cache disk space.
/// Verifies that the assembly cache directory has sufficient free space.
/// </summary>
public sealed class AssemblyCacheHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AssemblyCacheHealthCheck> _logger;
    
    // Thresholds
    private const long WarningThresholdBytes = 100 * 1024 * 1024; // 100 MB
    private const long UnhealthyThresholdBytes = 10 * 1024 * 1024; // 10 MB

    public AssemblyCacheHealthCheck(
        IConfiguration configuration,
        ILogger<AssemblyCacheHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get assembly cache path from configuration (or default)
            var cachePath = _configuration["Terrarium:AssemblyCachePath"] 
                ?? Path.Combine(Path.GetTempPath(), "terrarium-assemblies");

            // Ensure directory exists
            if (!Directory.Exists(cachePath))
            {
                Directory.CreateDirectory(cachePath);
                return Task.FromResult(HealthCheckResult.Healthy(
                    $"Assembly cache created at {cachePath}"));
            }

            // Get drive info
            var driveInfo = new DriveInfo(Path.GetPathRoot(cachePath)!);
            
            if (!driveInfo.IsReady)
            {
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Drive {driveInfo.Name} is not ready"));
            }

            var availableBytes = driveInfo.AvailableFreeSpace;
            var availableMB = availableBytes / (1024.0 * 1024.0);

            var data = new Dictionary<string, object>
            {
                ["cachePath"] = cachePath,
                ["availableSpaceMB"] = Math.Round(availableMB, 2),
                ["totalSpaceMB"] = Math.Round(driveInfo.TotalSize / (1024.0 * 1024.0), 2),
                ["drive"] = driveInfo.Name
            };

            if (availableBytes < UnhealthyThresholdBytes)
            {
                _logger.LogError(
                    "Assembly cache disk space critically low: {AvailableMB} MB available on {Drive}",
                    availableMB, driveInfo.Name);
                    
                return Task.FromResult(HealthCheckResult.Unhealthy(
                    $"Critically low disk space: {availableMB:F2} MB available",
                    data: data));
            }

            if (availableBytes < WarningThresholdBytes)
            {
                _logger.LogWarning(
                    "Assembly cache disk space low: {AvailableMB} MB available on {Drive}",
                    availableMB, driveInfo.Name);
                    
                return Task.FromResult(HealthCheckResult.Degraded(
                    $"Low disk space: {availableMB:F2} MB available",
                    data: data));
            }

            return Task.FromResult(HealthCheckResult.Healthy(
                $"Sufficient disk space: {availableMB:F2} MB available",
                data: data));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Assembly cache health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy(
                "Assembly cache check failed",
                exception: ex));
        }
    }
}
