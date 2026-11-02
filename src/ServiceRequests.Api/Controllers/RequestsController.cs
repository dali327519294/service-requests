using Microsoft.AspNetCore.Mvc;
using ServiceRequests.Application.Requests.Contracts;
using ServiceRequests.Application.Requests.Querying;

namespace ServiceRequests.Api.Controllers;

[ApiController]
[Route("api/requests")]
public sealed class RequestsController(RequestQueryService queries) : ControllerBase
{
    /// <summary>Searches, filters, sorts and pages the requests.</summary>
    [HttpGet]
    public Task<PagedResult<RequestListItemDto>> Search([FromQuery] RequestSearchQuery query, CancellationToken ct) =>
        queries.SearchAsync(query, ct);

    /// <summary>One request, with its rowVersion and the statuses it may move to.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<RequestDetailsDto>> GetById(int id, CancellationToken ct) =>
        await queries.GetByIdAsync(id, ct) is { } request ? request : NotFound();
}
