using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using Xunit;

namespace Terrarium.Server.Tests;

/// <summary>
/// Tests for throttle/rate-limiting behavior.
///
/// Legacy behavior (Server/Website/App_Code/Code/Throttle.cs):
///   - Each user (by IP) gets a max number of accesses per time window
///   - Throttled() returns true when user has hit max
///   - AddThrottle() returns false when limit reached
///   - After the throttle window expires, user can access again
///
/// Expected modern behavior:
///   - Rate limiting middleware returns 429 Too Many Requests when limit exceeded
///   - Response includes Retry-After header
///   - Normal requests within limits return success
///
/// These tests will pass once the throttle middleware is implemented.
/// Each test creates its own WebApplicationFactory to isolate throttle state.
/// </summary>
public class ThrottleTests
{
    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>();
    }

    [Fact]
    public async Task Single_Request_Should_Succeed()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/");

        Assert.True(response.IsSuccessStatusCode,
            $"Single request should succeed, got {response.StatusCode}");
    }

    [Fact]
    public async Task Multiple_Normal_Requests_Should_Succeed()
    {
        using var factory = CreateFactory();
        var client = factory.CreateClient();

        // A reasonable number of requests should all succeed
        for (int i = 0; i < 5; i++)
        {
            var response = await client.GetAsync("/");
            Assert.True(response.IsSuccessStatusCode,
                $"Request {i + 1} of 5 should succeed, got {response.StatusCode}");
        }
    }

    [Fact]
    public async Task Excessive_Requests_Should_Return_429()
    {
        // Legacy Throttle enforced per-user max count per time window.
        // The modern equivalent should return 429 when limit is exceeded.
        // Exact limit TBD by implementation (legacy default was configurable).
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        var got429 = false;

        for (int i = 0; i < 200; i++)
        {
            var response = await client.GetAsync("/");
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                got429 = true;
                break;
            }
        }

        Assert.True(got429,
            "Expected 429 Too Many Requests after excessive requests. " +
            "This test will pass once rate limiting middleware is added.");
    }

    [Fact]
    public async Task Throttled_Response_Contains_Retry_After_Header()
    {
        // When throttled, the response should include a Retry-After header
        // so the client knows when to try again.
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        HttpResponseMessage? throttledResponse = null;

        for (int i = 0; i < 200; i++)
        {
            var response = await client.GetAsync("/");
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throttledResponse = response;
                break;
            }
        }

        Assert.NotNull(throttledResponse);
        Assert.True(throttledResponse.Headers.Contains("Retry-After"),
            "Throttled response should include Retry-After header");
    }

    [Fact]
    public async Task Throttled_Response_Body_Contains_Rate_Limit_Message()
    {
        // The throttle response body should explain the rate limit.
        using var factory = CreateFactory();
        var client = factory.CreateClient();
        HttpResponseMessage? throttledResponse = null;

        for (int i = 0; i < 200; i++)
        {
            var response = await client.GetAsync("/");
            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                throttledResponse = response;
                break;
            }
        }

        Assert.NotNull(throttledResponse);
        var body = await throttledResponse.Content.ReadAsStringAsync();
        Assert.Contains("rate limit", body, StringComparison.OrdinalIgnoreCase);
    }
}
