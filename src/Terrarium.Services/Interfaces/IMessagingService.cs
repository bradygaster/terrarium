namespace Terrarium.Services.Interfaces;

public interface IMessagingService
{
    Task<string> GetWelcomeMessageAsync(CancellationToken cancellationToken = default);
    Task<string> GetMessageOfTheDayAsync(CancellationToken cancellationToken = default);
    Task<string> GetLatestVersionAsync(CancellationToken cancellationToken = default);
}
