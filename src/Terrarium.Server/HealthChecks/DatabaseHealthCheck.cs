using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Terrarium.Server.HealthChecks;

/// <summary>
/// Health check for database connectivity.
/// Currently a placeholder — will be implemented when database layer is added.
/// Marked as Degraded to indicate the feature is not yet implemented.
/// </summary>
public sealed class DatabaseHealthCheck : IHealthCheck
{
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(ILogger<DatabaseHealthCheck> logger)
    {
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // TODO: Sprint 13+ — Implement actual database connectivity check
        // For now, report degraded to indicate feature not yet implemented
        
        _logger.LogDebug("Database health check skipped (not yet implemented)");
        
        return Task.FromResult(HealthCheckResult.Healthy(
            "Database health check not yet implemented — passing by default"));
    }
}
