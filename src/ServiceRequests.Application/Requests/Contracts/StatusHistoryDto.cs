using ServiceRequests.Domain.Requests;

namespace ServiceRequests.Application.Requests.Contracts;

/// <summary>One status change, as the client sees it.</summary>
public sealed record StatusHistoryDto(RequestStatus PreviousStatus, RequestStatus NewStatus, DateTime ChangedAt, string ChangedBy);
