using Microsoft.EntityFrameworkCore;
using ServiceRequests.Application.Requests.Contracts;
using ServiceRequests.Application.Requests.Querying;
using ServiceRequests.Infrastructure.Persistence;

namespace ServiceRequests.Application.Requests.Summary;

public sealed class RequestSummaryService(ServiceRequestsDbContext db, SummaryCache cache)
{
    // Only the unfiltered summary is cached: it is the default view, and filter combinations are endless
    public ValueTask<RequestSummaryDto> GetAsync(RequestSearchQuery query, CancellationToken ct) =>
        RequestFilters.IsEmpty(query) ? cache.GetOrCreateAsync(token => ComputeAsync(query, token), ct) : ComputeAsync(query, ct);

    private async ValueTask<RequestSummaryDto> ComputeAsync(RequestSearchQuery query, CancellationToken ct)
    {
        var requests = db.Requests.AsNoTracking();
        var byStatus = await requests.Apply(query, ignoreStatus: true)
            .GroupBy(r => r.Status)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, ct);
        var byPriority = await requests.Apply(query, ignorePriority: true)
            .GroupBy(r => r.Priority)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(g => g.Key, g => g.Count, ct);

        // byStatus already has every filter except status, so the total is the sum over the chosen statuses - no third query
        var total = query.Status is { Count: > 0 } chosen
            ? byStatus.Where(s => chosen.Contains(s.Key)).Sum(s => s.Value)
            : byStatus.Values.Sum();

        return new RequestSummaryDto(total, WithZeros(byStatus), WithZeros(byPriority));
    }

    // SQL returns only the values that have rows; the client gets every value, zero included
    private static Dictionary<TEnum, int> WithZeros<TEnum>(Dictionary<TEnum, int> counts) where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>().ToDictionary(v => v, v => counts.GetValueOrDefault(v));
}
