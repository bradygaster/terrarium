using System.Diagnostics.Metrics;
using Terrarium.Net;
using Terrarium.Server;
using Terrarium.Server.HealthChecks;
using Terrarium.Server.Middleware;
using Terrarium.Server.Services;
using Terrarium.Server.Workers;
using Terrarium.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.Configure<ServerSettings>(
    builder.Configuration.GetSection("Terrarium"));
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ThrottleService>();
builder.Services.AddSingleton<IPopulationTrackingService, PopulationTrackingService>();
builder.Services.AddHostedService<NonPageServicesWorker>();
builder.Services.AddOpenApi();

// CORS policy for SignalR — allows local dev and Aspire service discovery origins
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(origin =>
            {
                var uri = new Uri(origin);
                return uri.Host == "localhost" || uri.Host == "127.0.0.1" || uri.Host.EndsWith(".internal");
            })
            .AllowAnyHeader()
            .WithMethods("GET", "POST")
            .AllowCredentials();
    });
});

// Register server-specific metrics with peer/species count providers
builder.Services.AddSingleton(sp =>
{
    var meterFactory = sp.GetRequiredService<IMeterFactory>();
    return new TerrariumMetrics(
        meterFactory,
        connectedPeerCountProvider: () => TerrariumHub.GetConnectedPeerCount(),
        activeSpeciesCountProvider: () => sp.GetRequiredService<IPopulationTrackingService>().GetActiveSpeciesCount());
});

// Add custom health checks
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database", tags: ["ready"])
    .AddCheck<SignalRHubHealthCheck>("signalr_hub", tags: ["ready"])
    .AddCheck<AssemblyCacheHealthCheck>("assembly_cache", tags: ["ready"]);

// SignalR configuration — optional Azure SignalR Service backplane
var signalRBuilder = builder.Services.AddSignalR(options =>
{
    options.MaximumReceiveMessageSize = 512 * 1024; // 512 KB for assembly transfers
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
});

// If Azure SignalR connection string is configured, use it for horizontal scaling
// Otherwise, fall back to in-process SignalR (local dev)
var signalRConnectionString = builder.Configuration.GetConnectionString("signalr");
if (!string.IsNullOrEmpty(signalRConnectionString))
{
    signalRBuilder.AddAzureSignalR(options =>
    {
        options.ServerStickyMode = Microsoft.Azure.SignalR.ServerStickyMode.Required;
    });
    builder.Services.AddSingleton<IHostedService>(sp =>
        new SignalRScalingService(sp.GetRequiredService<ILogger<SignalRScalingService>>()));
}

var app = builder.Build();

var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Terrarium.Server");

app.MapDefaultEndpoints();
app.MapOpenApi();
app.UseMiddleware<ThrottleMiddleware>();

app.UseCors();

app.MapGet("/", () => "Terrarium Server");

app.MapHub<TerrariumHub>("/hubs/terrarium");

startupLogger.LogInformation("Terrarium Server started — hub mapped at /hubs/terrarium");

app.MapGroup("/api/messaging")
    .MapMessagingEndpoints();

app.MapGroup("/api/discovery")
    .MapPeerDiscoveryEndpoints();

app.MapGroup("/api/watson")
    .MapWatsonEndpoints();

app.MapGroup("/api/bugs")
    .MapBugEndpoints();

app.MapGroup("/api/species")
    .MapSpeciesEndpoints();

app.MapGroup("/api/reporting")
    .MapReportingEndpoints();

app.MapGroup("/api/charts")
    .MapChartsEndpoints();

app.MapGroup("/api/usage")
    .MapUsageEndpoints();

app.Run();

// Make Program visible to integration tests using WebApplicationFactory<Program>
public partial class Program { }
