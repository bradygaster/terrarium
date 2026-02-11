using Terrarium.Server;
using Terrarium.Server.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.Configure<ServerSettings>(
    builder.Configuration.GetSection("Terrarium"));
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ThrottleService>();

var app = builder.Build();

app.MapDefaultEndpoints();
app.UseMiddleware<ThrottleMiddleware>();

app.MapGet("/", () => "Terrarium Server");

app.MapGroup("/api/messaging")
    .MapMessagingEndpoints();

app.MapGroup("/api/discovery")
    .MapPeerDiscoveryEndpoints();

app.MapGroup("/api/watson")
    .MapWatsonEndpoints();

app.MapGroup("/api/bugs")
    .MapBugEndpoints();

app.Run();

// Make Program visible to integration tests using WebApplicationFactory<Program>
public partial class Program { }
