namespace Terrarium.Server.Middleware;

/// <summary>
/// Middleware that applies per-IP throttling to incoming requests.
/// Wraps ThrottleService to provide request-pipeline integration.
/// This replaces the legacy Throttle.cs which relied on ASP.NET HttpContext.Current.Cache.
/// </summary>
public sealed class ThrottleMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ThrottleService _throttle;

    // Default: 60 requests per IP in a sliding 1-minute window
    private const int DefaultMaxRequests = 60;
    private static readonly TimeSpan DefaultWindow = TimeSpan.FromMinutes(1);
    private const string ThrottleName = "global";

    public ThrottleMiddleware(RequestDelegate next, ThrottleService throttle)
    {
        _next = next;
        _throttle = throttle;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        if (_throttle.IsThrottled(ip, ThrottleName))
        {
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = DefaultWindow.TotalSeconds.ToString("F0");
            await context.Response.WriteAsync("Rate limit exceeded. Try again later.");
            return;
        }

        _throttle.AddThrottle(ip, ThrottleName, DefaultMaxRequests, DefaultWindow);

        await _next(context);
    }
}
