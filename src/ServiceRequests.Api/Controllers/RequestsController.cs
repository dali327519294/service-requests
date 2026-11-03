using Microsoft.AspNetCore.Mvc;
using ServiceRequests.Api.Logging;
using ServiceRequests.Application.Requests.Commands;
using ServiceRequests.Application.Requests.Contracts;
using ServiceRequests.Application.Requests.Querying;
using ServiceRequests.Application.Requests.Summary;

namespace ServiceRequests.Api.Controllers;

[ApiController]
[Route("api/requests")]
public sealed class RequestsController(RequestQueryService queries, RequestSummaryService summaries,
    RequestStatusService statuses, ILogger<RequestsController> logger) : ControllerBase
{
    /// <summary>Searches, filters, sorts and pages the requests.</summary>
    [HttpGet]
    public Task<PagedResult<RequestListItemDto>> Search([FromQuery] RequestSearchQuery query, CancellationToken ct) =>
        queries.SearchAsync(query, ct);

    /// <summary>Counts by status and by priority for the same filters as the list.</summary>
    [HttpGet("summary")]
    public async Task<RequestSummaryDto> Summary([FromQuery] RequestSearchQuery query, CancellationToken ct) =>
        await summaries.GetAsync(query, ct);

    /// <summary>One request, with its rowVersion and the statuses it may move to.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RequestDetailsDto>> GetById(int id, CancellationToken ct) =>
        await queries.GetByIdAsync(id, ct) is { } request ? request : NotFound();

    /// <summary>
    /// Changes the status. The body carries the rowVersion the client saw; if the request changed since,
    /// the answer is 409 with its current state, so the client can show it without another GET.
    /// </summary>
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<RequestDetailsDto>> UpdateStatus(int id, UpdateStatusRequest update, CancellationToken ct)
    {
        var outcome = await statuses.UpdateStatusAsync(id, update, ct);
        switch (outcome)
        {
            case UpdateStatusOutcome.Updated updated:
                Log.StatusChanged(logger, id, updated.PreviousStatus, updated.Request.Status);
                return updated.Request;
            case UpdateStatusOutcome.NotFound:
                return NotFound();
            case UpdateStatusOutcome.Conflict conflict:
                Log.StatusConflict(logger, id);
                return ProblemWith(StatusCodes.Status409Conflict, "The request was changed by someone else.",
                    "Review its current state and try again.", "current", conflict.Current);
            case UpdateStatusOutcome.InvalidTransition invalid:
                return ProblemWith(StatusCodes.Status400BadRequest, "The status change is not allowed.",
                    $"A request in status {invalid.From} cannot move to {invalid.To}.", "allowedTransitions", invalid.Allowed);
            default:
                throw new InvalidOperationException($"Unhandled outcome {outcome.GetType().Name}.");
        }
    }

    /// <summary>
    /// Changes the status of up to 100 requests. Each request gets its own outcome, in the order of the request,
    /// and the answer is 200 even when none succeeded. A malformed batch is rejected whole with 400.
    /// </summary>
    [HttpPost("bulk-status")]
    public async Task<BulkUpdateResult> BulkUpdateStatus(BulkUpdateStatusRequest bulk, CancellationToken ct)
    {
        var result = await statuses.BulkUpdateAsync(bulk, ct);
        Log.BulkStatusUpdated(logger, result.Requested, result.Succeeded, result.Failed);
        return result;
    }

    /// <summary>The status changes of one request, newest first.</summary>
    [HttpGet("{id:int}/history")]
    public async Task<ActionResult<IReadOnlyList<StatusHistoryDto>>> History(int id, CancellationToken ct) =>
        await queries.GetHistoryAsync(id, ct) is { } history ? Ok(history) : NotFound();

    // ProblemDetails from the framework factory (it adds traceId), plus the one field the client acts on
    private ObjectResult ProblemWith(int status, string title, string detail, string extension, object value)
    {
        var problem = ProblemDetailsFactory.CreateProblemDetails(HttpContext, status, title, detail: detail);
        problem.Extensions[extension] = value;
        return new ObjectResult(problem) { StatusCode = status };
    }
}
