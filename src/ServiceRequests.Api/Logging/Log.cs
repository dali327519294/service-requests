namespace ServiceRequests.Api.Logging;

/// <summary>All API log messages in one place, generated at compile time.</summary>
internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Request {Method} {Path} was cancelled by the client")]
    public static partial void RequestCancelled(ILogger logger, string method, PathString path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Request rejected: {Reason}")]
    public static partial void RequestRejected(ILogger logger, string reason);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Validation failed for {Fields}")]
    public static partial void ValidationFailed(ILogger logger, string fields);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception, trace id {TraceId}")]
    public static partial void UnhandledException(ILogger logger, Exception exception, string traceId);
}
