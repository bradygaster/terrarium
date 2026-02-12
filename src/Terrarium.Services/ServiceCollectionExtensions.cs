using Microsoft.Extensions.DependencyInjection;
using Terrarium.Services.Clients;
using Terrarium.Services.Interfaces;

namespace Terrarium.Services;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Terrarium service clients with the DI container.
    /// Configures named HttpClient instances for each service with retry policy.
    /// </summary>
    public static IServiceCollection AddTerrariumServices(this IServiceCollection services, Uri baseAddress)
    {
        services.AddHttpClient<IMessagingService, MessagingServiceClient>(client =>
            client.BaseAddress = new Uri(baseAddress, "api/"))
            .AddStandardResilienceHandler();

        services.AddHttpClient<IPeerDiscoveryService, PeerDiscoveryServiceClient>(client =>
            client.BaseAddress = new Uri(baseAddress, "api/"))
            .AddStandardResilienceHandler();

        services.AddHttpClient<ISpeciesService, SpeciesServiceClient>(client =>
            client.BaseAddress = new Uri(baseAddress, "api/"))
            .AddStandardResilienceHandler();

        services.AddHttpClient<IPopulationService, PopulationServiceClient>(client =>
            client.BaseAddress = new Uri(baseAddress, "api/"))
            .AddStandardResilienceHandler();

        services.AddHttpClient<IReportingService, ReportingServiceClient>(client =>
            client.BaseAddress = new Uri(baseAddress, "api/"))
            .AddStandardResilienceHandler();

        services.AddHttpClient<IChartService, ChartServiceClient>(client =>
            client.BaseAddress = new Uri(baseAddress, "api/"))
            .AddStandardResilienceHandler();

        services.AddHttpClient<IUsageService, UsageServiceClient>(client =>
            client.BaseAddress = new Uri(baseAddress, "api/"))
            .AddStandardResilienceHandler();

        services.AddHttpClient<IWatsonService, WatsonServiceClient>(client =>
            client.BaseAddress = new Uri(baseAddress, "api/"))
            .AddStandardResilienceHandler();

        return services;
    }

    /// <summary>
    /// Registers all Terrarium service clients using Aspire service discovery.
    /// Use this overload when the server is registered in Aspire as a named resource.
    /// </summary>
    public static IServiceCollection AddTerrariumServices(this IServiceCollection services, string serviceName)
    {
        return services.AddTerrariumServices(new Uri($"https+http://{serviceName}"));
    }
}
