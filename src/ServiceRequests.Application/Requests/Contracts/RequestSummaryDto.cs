using ServiceRequests.Domain.Requests;

namespace ServiceRequests.Application.Requests.Contracts;

/// <summary>Faceted counts: each facet applies every filter except its own; every enum value is present, zero included.</summary>
public sealed record RequestSummaryDto(int Total, IReadOnlyDictionary<RequestStatus, int> ByStatus,
    IReadOnlyDictionary<RequestPriority, int> ByPriority);
