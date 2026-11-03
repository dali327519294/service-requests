using Microsoft.Extensions.Caching.Hybrid;
using ServiceRequests.Application.Requests.Contracts;

namespace ServiceRequests.Application.Requests.Summary;

/// <summary>
/// Caches the unfiltered summary. Invalidate bumps a generation number that is part of the key, so a value
/// computed before an invalidation is stored under a key that nobody reads any more.
/// </summary>
public sealed class SummaryCache(HybridCache cache, TimeSpan timeToLive)
{
    private readonly HybridCacheEntryOptions _options = new() { Expiration = timeToLive, LocalCacheExpiration = timeToLive };
    private long _generation;

    public ValueTask<RequestSummaryDto> GetOrCreateAsync(Func<CancellationToken, ValueTask<RequestSummaryDto>> factory,
        CancellationToken ct)
    {
        // The key is fixed before the factory runs - that is what makes a late result harmless
        var key = $"requests:summary:v{Interlocked.Read(ref _generation)}";
        return cache.GetOrCreateAsync(key, factory, _options, cancellationToken: ct);
    }

    /// <summary>Call after the change is committed, never before: a read in between would cache old data under the new key.</summary>
    public void Invalidate() => Interlocked.Increment(ref _generation);
}
