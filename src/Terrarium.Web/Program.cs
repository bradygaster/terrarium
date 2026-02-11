using Terrarium.Configuration;
using Terrarium.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddTerrariumConfiguration();

builder.Services.AddHttpClient("terrarium-server", client =>
{
    client.BaseAddress = new Uri("https+http://server");
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddSignalR();

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
