using Microsoft.AspNetCore.Mvc;
using ServiceRequests.Application.Requests.Contracts;
using ServiceRequests.Application.Requests.Querying;
using ServiceRequests.Application.Requests.Summary;

namespace ServiceRequests.Api.Controllers;

[ApiController]
[Route("api/requests")]
public sealed class RequestsController(RequestQueryService queries, RequestSummaryService summaries) : ControllerBase
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
}
