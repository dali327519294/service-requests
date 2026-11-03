using ServiceRequests.Application.Requests.Contracts;
using ServiceRequests.Domain.Requests;

namespace ServiceRequests.Application.Requests.Querying;

/// <summary>The list filters, shared by the search and the summary.</summary>
public static class RequestFilters
{
    /// <param name="ignoreStatus">For the status facet: count every status under the other filters.</param>
    /// <param name="ignorePriority">For the priority facet, the same.</param>
    public static IQueryable<ServiceRequest> Apply(this IQueryable<ServiceRequest> requests, RequestSearchQuery query,
        bool ignoreStatus = false, bool ignorePriority = false)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // EF Core escapes %, _ and [ in the parameter, so the text is matched literally
            var search = query.Search.Trim();
            requests = requests.Where(r => r.Title.Contains(search) || r.OrganizationName.Contains(search));
        }

        if (!ignoreStatus && query.Status is { Count: > 0 } statuses)
        {
            requests = requests.Where(r => statuses.Contains(r.Status));
        }

        if (!ignorePriority && query.Priority is { Count: > 0 } priorities)
        {
            requests = requests.Where(r => priorities.Contains(r.Priority));
        }

        if (!string.IsNullOrWhiteSpace(query.OrganizationName))
        {
            var prefix = query.OrganizationName.Trim();
            requests = requests.Where(r => r.OrganizationName.StartsWith(prefix));
        }

        if (!string.IsNullOrWhiteSpace(query.AssignedTo))
        {
            var assignedTo = query.AssignedTo.Trim();
            requests = requests.Where(r => r.AssignedTo == assignedTo);
        }

        if (query.CreatedFrom is { } from)
        {
            var fromUtc = from.UtcDateTime;
            requests = requests.Where(r => r.CreatedAt >= fromUtc);
        }

        if (query.CreatedTo is { } to)
        {
            var toUtc = to.UtcDateTime;
            requests = requests.Where(r => r.CreatedAt < toUtc);
        }

        return requests;
    }

    /// <summary>True when no filter is set. Paging, sorting and blank text are not filters, the same as in Apply.</summary>
    public static bool IsEmpty(RequestSearchQuery query) =>
        string.IsNullOrWhiteSpace(query.Search)
        && query.Status is not { Count: > 0 }
        && query.Priority is not { Count: > 0 }
        && string.IsNullOrWhiteSpace(query.OrganizationName)
        && string.IsNullOrWhiteSpace(query.AssignedTo)
        && query.CreatedFrom is null
        && query.CreatedTo is null;
}
