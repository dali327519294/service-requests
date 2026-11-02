namespace ServiceRequests.Application.Requests.Contracts;

/// <summary>The only fields the list can be sorted by.</summary>
public enum RequestSortField
{
    CreatedAt,
    UpdatedAt,
    Priority,
    Status,
    OrganizationName,
}
