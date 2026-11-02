using ServiceRequests.Domain.Requests;

namespace ServiceRequests.Application.Requests.Contracts;

/// <summary>One row of the request list; RowVersion lets a bulk update start from the list.</summary>
public sealed record RequestListItemDto(int Id, string Title, string OrganizationName, RequestStatus Status,
    RequestPriority Priority, string? AssignedTo, DateTime CreatedAt, DateTime UpdatedAt, string RowVersion);
