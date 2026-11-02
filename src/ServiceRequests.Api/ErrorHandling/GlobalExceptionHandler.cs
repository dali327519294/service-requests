using Microsoft.AspNetCore.Diagnostics;
using ServiceRequests.Api.Logging;
using ServiceRequests.Application.Common;

namespace ServiceRequests.Api.ErrorHandling;

/// <summary>Turns every exception into ProblemDetails; internal details go to the log only.</summary>
internal sealed class GlobalExceptionHandler(IProblemDetailsService problemDetails, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        // The client went away: there is no one to answer, and it is not an error
        if (exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested)
        {
            Log.RequestCancelled(logger, context.Request.Method, context.Request.Path);
            return true;
        }

        var (status, title, detail) = exception switch
        {
            MissingUserException missingUser => (StatusCodes.Status400BadRequest, "The request has no valid user.", missingUser.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null),
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            Log.UnhandledException(logger, exception, context.TraceIdentifier);
        }
        else
        {
            Log.RequestRejected(logger, title);
        }

        context.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = { Status = status, Title = title, Detail = detail },
        });
    }
}
