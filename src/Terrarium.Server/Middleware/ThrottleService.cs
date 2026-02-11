using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace Terrarium.Server.Middleware;

/// <summary>
/// In-memory per-user rate limiter ported from the legacy Throttle.cs.
/// Uses IMemoryCache for TTL-based expiration instead of ASP.NET Cache.
/// </summary>
public sealed class ThrottleService
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ThrottleData>> _throttledUsers = new();
    private readonly IMemoryCache _cache;

    public ThrottleService(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Returns true if the user has already reached the max allowed accesses for the named throttle.
    /// </summary>
    public bool IsThrottled(string user, string throttle)
    {
        if (!_throttledUsers.TryGetValue(user, out var throttles))
            return false;

        if (!throttles.TryGetValue(throttle, out var td))
            return false;

        return td.Current >= td.Max;
    }

    /// <summary>
    /// Increments the throttle counter for a user/throttle pair.
    /// Returns true if the access is allowed; false if the user is already throttled.
    /// </summary>
    public bool AddThrottle(string user, string throttle, int max, TimeSpan duration)
    {
        var throttles = _throttledUsers.GetOrAdd(user, _ => new ConcurrentDictionary<string, ThrottleData>());
        var td = throttles.GetOrAdd(throttle, _ => new ThrottleData { Max = max });

        if (td.Current >= td.Max)
            return false;

        Interlocked.Increment(ref td.Current);

        // Cache entry whose eviction decrements the counter, mirroring the legacy CacheItemRemovedCallback
        var cacheKey = $"{user}:{throttle}:{DateTime.UtcNow.Ticks}";
        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(duration)
            .RegisterPostEvictionCallback((_, value, _, _) =>
            {
                if (value is ThrottleData evictedTd)
                    Interlocked.Decrement(ref evictedTd.Current);
            });

        _cache.Set(cacheKey, td, options);
        return true;
    }

    private sealed class ThrottleData
    {
        public int Max;
        public int Current;
    }
}
