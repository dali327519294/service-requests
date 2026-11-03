namespace ServiceRequests.Application.Requests.Contracts;

public enum BulkOutcome
{
    Updated,
    NotFound,
    Conflict,
    InvalidTransition,
}

/// <summary>The result for one request. RowVersion is the new version when it was updated, so the client can go on.</summary>
public sealed record BulkItemResult(int Id, BulkOutcome Outcome, string? Error, string? RowVersion);

/// <summary>Results are in the order of the request.</summary>
public sealed record BulkUpdateResult(int Requested, int Succeeded, int Failed, IReadOnlyList<BulkItemResult> Results);
