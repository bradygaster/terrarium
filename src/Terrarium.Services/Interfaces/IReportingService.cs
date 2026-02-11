using Terrarium.Services.Models;

namespace Terrarium.Services.Interfaces;

public interface IReportingService
{
    Task ReportBugAsync(BugReport report, CancellationToken cancellationToken = default);
    Task ReportErrorAsync(string errorData, CancellationToken cancellationToken = default);
    Task ReportUsageAsync(UsageData data, CancellationToken cancellationToken = default);
}
