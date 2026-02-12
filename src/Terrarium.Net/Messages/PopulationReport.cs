namespace Terrarium.Net;

/// <summary>
/// Population statistics for an ecosystem, reported by clients.
/// Replaces legacy ReportingService SOAP-based population batching.
/// </summary>
public sealed class PopulationReport
{
    /// <summary>
    /// The ecosystem this report belongs to.
    /// </summary>
    public required string EcosystemId { get; init; }

    /// <summary>
    /// Tick number when this report was generated.
    /// </summary>
    public required long TickNumber { get; init; }

    /// <summary>
    /// Per-species population breakdown.
    /// </summary>
    public required IReadOnlyList<SpeciesPopulation> Species { get; init; }

    /// <summary>
    /// Total organism count at report time.
    /// </summary>
    public int TotalOrganisms { get; init; }

    /// <summary>
    /// UTC timestamp of report generation.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// Population data for a single species within a population report.
/// </summary>
public sealed class SpeciesPopulation
{
    /// <summary>
    /// Name of the species.
    /// </summary>
    public required string SpeciesName { get; init; }

    /// <summary>
    /// Current population count.
    /// </summary>
    public int Population { get; init; }

    /// <summary>
    /// Births since last report.
    /// </summary>
    public int Births { get; init; }

    /// <summary>
    /// Deaths since last report.
    /// </summary>
    public int Deaths { get; init; }
}
