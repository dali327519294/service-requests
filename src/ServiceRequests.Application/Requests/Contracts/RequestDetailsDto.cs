using ServiceRequests.Domain.Requests;

namespace ServiceRequests.Application.Requests.Contracts;

/// <summary>A single request, with the statuses it may move to next.</summary>
public sealed record RequestDetailsDto(int Id, string Title, string OrganizationName, RequestStatus Status,
    RequestPriority Priority, string? AssignedTo, DateTime CreatedAt, DateTime UpdatedAt, string RowVersion,
    IReadOnlyList<RequestStatus> AllowedTransitions);
