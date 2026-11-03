using ServiceRequests.Domain.Requests;

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

    [LoggerMessage(Level = LogLevel.Information, Message = "Request {RequestId} status changed from {From} to {To}")]
    public static partial void StatusChanged(ILogger logger, int requestId, RequestStatus from, RequestStatus to);

    // A conflict is an expected business event, not a fault: Information, not Warning
    [LoggerMessage(Level = LogLevel.Information, Message = "Request {RequestId} status update conflicted with a newer version")]
    public static partial void StatusConflict(ILogger logger, int requestId);

    [LoggerMessage(Level = LogLevel.Information, Message = "Bulk status update: {Requested} requested, {Succeeded} updated, {Failed} failed")]
    public static partial void BulkStatusUpdated(ILogger logger, int requested, int succeeded, int failed);

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception, trace id {TraceId}")]
    public static partial void UnhandledException(ILogger logger, Exception exception, string traceId);
}
