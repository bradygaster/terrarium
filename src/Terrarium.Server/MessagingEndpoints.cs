using Microsoft.Extensions.Options;

namespace Terrarium.Server;

/// <summary>
/// Minimal API endpoints for the Messaging service.
/// Ported from Server/Website/App_Code/Messaging/Messaging.asmx.cs.
/// </summary>
public static class MessagingEndpoints
{
    public static RouteGroupBuilder MapMessagingEndpoints(this RouteGroupBuilder group)
    {
        group.WithTags("Messaging");

        group.MapGet("/welcome", (IOptions<ServerSettings> settings) =>
        {
            var message = settings.Value.WelcomeMessage;
            return Results.Ok(new { message });
        })
        .WithName("GetWelcomeMessage");

        group.MapGet("/motd", (IOptions<ServerSettings> settings) =>
        {
            var message = settings.Value.MOTD;
            return Results.Ok(new { message });
        })
        .WithName("GetMotd");

        group.MapGet("/version", (IOptions<ServerSettings> settings) =>
        {
            var version = settings.Value.LatestVersion;
            return Results.Ok(new { version });
        })
        .WithName("GetLatestVersion");

        return group;
    }
}
