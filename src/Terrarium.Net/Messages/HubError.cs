namespace Terrarium.Net;

/// <summary>
/// Error message sent from hub to client via ReceiveError callback.
/// Replaces throwing exceptions from hub methods (which kills the connection).
/// </summary>
public sealed class HubError
{
    /// <summary>
    /// Machine-readable error code (e.g., UNKNOWN_SPECIES, RATE_LIMITED).
    /// </summary>
    public required string Code { get; init; }

    /// <summary>
    /// Human-readable error description.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Whether the error is transient and the client should retry.
    /// </summary>
    public bool IsTransient { get; init; }

    /// <summary>
    /// Milliseconds the client should wait before retrying (if transient).
    /// </summary>
    public int? RetryAfterMs { get; init; }

    /// <summary>
    /// Correlation ID for tracing the error across logs.
    /// </summary>
    public string? CorrelationId { get; init; }
}
