using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using ServiceRequests.Application.Requests;
using ServiceRequests.Application.Requests.Contracts;
using ServiceRequests.Application.Requests.Summary;
using ServiceRequests.Domain.Requests;
using ServiceRequests.IntegrationTests.Infrastructure;

namespace ServiceRequests.IntegrationTests.Api;

public class SummaryCacheTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private SummaryCache Cache => factory.Services.GetRequiredService<SummaryCache>();

    [Fact]
    public async Task Unfiltered_summary_is_served_from_cache()
    {
        Cache.Invalidate();   // start empty, whatever ran before
        var client = factory.CreateClient();
        var first = (await client.GetFromJsonAsync<RequestSummaryDto>("/api/requests/summary", Json))!;

        // Written straight to the database, so nothing tells the cache
        await TestData.AddRequestAsync(factory);
        var second = (await client.GetFromJsonAsync<RequestSummaryDto>("/api/requests/summary", Json))!;
        var list = (await client.GetFromJsonAsync<PagedResult<RequestListItemDto>>("/api/requests?pageSize=1", Json))!;

        Assert.Equal(first.Total, second.Total);
        Assert.Equal(first.Total + 1, list.TotalCount);   // the row is really there
    }

    [Fact]
    public async Task Status_update_invalidates_cached_summary()
    {
        Cache.Invalidate();
        var request = await TestData.AddRequestAsync(factory, status: RequestStatus.InProgress);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Name", "dana");
        var before = (await client.GetFromJsonAsync<RequestSummaryDto>("/api/requests/summary", Json))!;

        await client.PatchAsJsonAsync($"/api/requests/{request.Id}/status",
            new { status = "Completed", rowVersion = RowVersionCodec.Encode(request.RowVersion) });
        var after = (await client.GetFromJsonAsync<RequestSummaryDto>("/api/requests/summary", Json))!;

        Assert.Equal(before.ByStatus[RequestStatus.InProgress] - 1, after.ByStatus[RequestStatus.InProgress]);
        Assert.Equal(before.ByStatus[RequestStatus.Completed] + 1, after.ByStatus[RequestStatus.Completed]);
    }

    [Fact]
    public async Task Bulk_update_invalidates_cached_summary()
    {
        Cache.Invalidate();
        var request = await TestData.AddRequestAsync(factory, status: RequestStatus.InProgress);
        var client = UserClient();
        var before = (await client.GetFromJsonAsync<RequestSummaryDto>("/api/requests/summary", Json))!;

        await client.PostAsJsonAsync("/api/requests/bulk-status", new
        {
            status = "Completed",
            items = new[] { new { id = request.Id, rowVersion = RowVersionCodec.Encode(request.RowVersion) } },
        });
        var after = (await client.GetFromJsonAsync<RequestSummaryDto>("/api/requests/summary", Json))!;

        Assert.Equal(before.ByStatus[RequestStatus.Completed] + 1, after.ByStatus[RequestStatus.Completed]);
    }

    [Fact]
    public async Task Bulk_with_no_success_keeps_the_cached_summary()
    {
        Cache.Invalidate();
        var client = UserClient();
        var before = (await client.GetFromJsonAsync<RequestSummaryDto>("/api/requests/summary", Json))!;
        await TestData.AddRequestAsync(factory);   // straight to the database: only an invalidation would reveal it

        await client.PostAsJsonAsync("/api/requests/bulk-status", new
        {
            status = "Completed",
            items = new[] { new { id = 999999, rowVersion = RowVersionCodec.Encode(new byte[8]) } },
        });
        var after = (await client.GetFromJsonAsync<RequestSummaryDto>("/api/requests/summary", Json))!;

        Assert.Equal(before.Total, after.Total);   // nothing changed, so the cache was not thrown away
    }

    [Fact]
    public async Task Value_computed_during_invalidation_is_not_served()
    {
        Cache.Invalidate();
        var calls = 0;
        var release = new TaskCompletionSource();

        // A slow computation that is still running when a status changes
        var inFlight = Cache.GetOrCreateAsync(async _ =>
        {
            Interlocked.Increment(ref calls);
            await release.Task;
            return Summary(total: 1);
        }, CancellationToken.None).AsTask();
        Cache.Invalidate();
        release.SetResult();
        await inFlight;

        var next = await Cache.GetOrCreateAsync(_ =>
        {
            Interlocked.Increment(ref calls);
            return ValueTask.FromResult(Summary(total: 2));
        }, CancellationToken.None);

        Assert.Equal(2, calls);        // the old value was not reused
        Assert.Equal(2, next.Total);
    }

    private HttpClient UserClient()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Name", "dana");
        return client;
    }

    private static RequestSummaryDto Summary(int total) => new(total,
        Enum.GetValues<RequestStatus>().ToDictionary(s => s, _ => 0),
        Enum.GetValues<RequestPriority>().ToDictionary(p => p, _ => 0));
}
