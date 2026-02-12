using Terrarium.Configuration;
using Terrarium.Game;
using Terrarium.Game.Rendering;
using Terrarium.Services;
using Terrarium.Web;
using Terrarium.Web.Components;
using Terrarium.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddTerrariumConfiguration();
builder.Services.AddTerrariumGameEngine();
builder.Services.AddTerrariumNetworking();
builder.Services.AddTerrariumRenderer();
builder.Services.AddTerrariumServices("server");

// Engine renderer stub — the Web app renders via IGameRenderer/Canvas,
// but GameRenderBridge requires IEngineRenderer in DI.
builder.Services.AddSingleton<IEngineRenderer, NoOpEngineRenderer>();

builder.Services.AddHttpClient("terrarium-server", client =>
{
    client.BaseAddress = new Uri("https+http://server");
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();
builder.Services.AddSingleton<TerrariumHubClient>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapDefaultEndpoints();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
