using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ServiceRequests.Application.Requests.Contracts;
using ServiceRequests.Domain.Requests;
using ServiceRequests.Infrastructure.Persistence;

namespace ServiceRequests.Application.Requests.Querying;

public sealed class RequestQueryService(ServiceRequestsDbContext db)
{
    public async Task<PagedResult<RequestListItemDto>> SearchAsync(RequestSearchQuery query, CancellationToken ct)
    {
        var filtered = db.Requests.AsNoTracking().Apply(query);
        var totalCount = await filtered.CountAsync(ct);

        // long: (page - 1) * pageSize overflows int for very large page numbers
        var skip = (long)(query.Page - 1) * query.PageSize;
        if (skip >= totalCount)
        {
            return new PagedResult<RequestListItemDto>([], query.Page, query.PageSize, totalCount);
        }

        var items = await Sort(filtered, query.SortBy, query.SortDirection)
            .Skip((int)skip)
            .Take(query.PageSize)
            .Select(r => new RequestListItemDto(r.Id, r.Title, r.OrganizationName, r.Status, r.Priority,
                r.AssignedTo, r.CreatedAt, r.UpdatedAt, RowVersionCodec.Encode(r.RowVersion)))
            .ToListAsync(ct);

        return new PagedResult<RequestListItemDto>(items, query.Page, query.PageSize, totalCount);
    }

    public async Task<RequestDetailsDto?> GetByIdAsync(int id, CancellationToken ct) =>
        (await db.Requests.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id, ct))?.ToDetails();

    /// <summary>Newest first. Null when the request does not exist; an empty list means it never changed.</summary>
    public async Task<IReadOnlyList<StatusHistoryDto>?> GetHistoryAsync(int id, CancellationToken ct)
    {
        if (!await db.Requests.AnyAsync(r => r.Id == id, ct))
        {
            return null;
        }

        return await db.StatusHistory.AsNoTracking()
            .Where(h => h.RequestId == id)
            .OrderByDescending(h => h.ChangedAt)
            .ThenByDescending(h => h.Id)   // keeps changes from the same millisecond in order
            .Select(h => new StatusHistoryDto(h.PreviousStatus, h.NewStatus, h.ChangedAt, h.ChangedBy))
            .ToListAsync(ct);
    }

    private static IOrderedQueryable<ServiceRequest> Sort(IQueryable<ServiceRequest> requests, RequestSortField field, SortDirection direction)
    {
        var descending = direction == SortDirection.Desc;
        var sorted = field switch
        {
            RequestSortField.CreatedAt => OrderBy(requests, r => r.CreatedAt, descending),
            RequestSortField.UpdatedAt => OrderBy(requests, r => r.UpdatedAt, descending),
            RequestSortField.Priority => OrderBy(requests, r => r.Priority, descending),
            RequestSortField.Status => OrderBy(requests, r => r.Status, descending),
            RequestSortField.OrganizationName => OrderBy(requests, r => r.OrganizationName, descending),
            _ => throw new ArgumentOutOfRangeException(nameof(field), field, null),
        };

        // Id breaks ties, so a row never shows up on two pages or on none
        return descending ? sorted.ThenByDescending(r => r.Id) : sorted.ThenBy(r => r.Id);
    }

    private static IOrderedQueryable<ServiceRequest> OrderBy<TKey>(IQueryable<ServiceRequest> requests,
        Expression<Func<ServiceRequest, TKey>> key, bool descending) =>
        descending ? requests.OrderByDescending(key) : requests.OrderBy(key);
}
