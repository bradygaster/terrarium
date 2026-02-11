// Copyright (c) Microsoft Corporation.  All rights reserved.

using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace Terrarium.Configuration;

/// <summary>
/// Centralized error logging using <see cref="ILogger"/> instead of the legacy
/// file-based / DataSet-based approach.
/// </summary>
public sealed class ErrorLog
{
    private readonly ILogger<ErrorLog> _logger;

    public ErrorLog(ILogger<ErrorLog> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Log a handled exception at Debug level.
    /// </summary>
    public void LogHandledException(Exception e)
    {
        _logger.LogDebug(e, "Handled exception: {Message}", FormatException(e));
    }

    /// <summary>
    /// Log a failed assertion at Error level.
    /// </summary>
    public void LogFailedAssertion(string message, string traces = "")
    {
        _logger.LogError("Assertion failure: {Message} Traces: {Traces}", message, traces);
    }

    /// <summary>
    /// Formats an exception with special handling for <see cref="SocketException"/>.
    /// </summary>
    public static string FormatException(Exception e)
    {
        if (e is SocketException socketEx)
        {
            return $"SocketException({socketEx.SocketErrorCode}): {e.Message}\r\n{e.StackTrace}";
        }

        return e.ToString();
    }
}
