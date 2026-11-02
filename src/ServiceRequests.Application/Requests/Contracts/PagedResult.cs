namespace ServiceRequests.Application.Requests.Contracts;

/// <summary>One page of results, with the total that matches the same filters.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
