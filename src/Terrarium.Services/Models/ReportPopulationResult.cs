namespace Terrarium.Services.Models;

public enum ReportingReturnCode
{
    Success = 0,
    AlreadyExists = 1,
    ServerDown = 2,
    NodeTimedOut = 3,
    NodeCorrupted = 4,
    OrganismBlacklisted = 5
}

public sealed class ReportPopulationResult
{
    public ReportingReturnCode ReturnCode { get; init; }
}
