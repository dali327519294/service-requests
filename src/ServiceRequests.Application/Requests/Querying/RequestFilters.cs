using ServiceRequests.Application.Requests.Contracts;
using ServiceRequests.Domain.Requests;

namespace ServiceRequests.Application.Requests.Querying;

/// <summary>The list filters, shared by the search and (later) the summary.</summary>
public static class RequestFilters
{
    public static IQueryable<ServiceRequest> Apply(this IQueryable<ServiceRequest> requests, RequestSearchQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            // EF Core escapes %, _ and [ in the parameter, so the text is matched literally
            var search = query.Search.Trim();
            requests = requests.Where(r => r.Title.Contains(search) || r.OrganizationName.Contains(search));
        }

        if (query.Status is { Count: > 0 } statuses)
        {
            requests = requests.Where(r => statuses.Contains(r.Status));
        }

        if (query.Priority is { Count: > 0 } priorities)
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
}
