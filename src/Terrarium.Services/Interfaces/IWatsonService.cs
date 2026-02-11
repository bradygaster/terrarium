using Terrarium.Services.Models;

namespace Terrarium.Services.Interfaces;

public interface IWatsonService
{
    Task<bool> ReportErrorAsync(WatsonReport report, CancellationToken cancellationToken = default);
}
