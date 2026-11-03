using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using ServiceRequests.Application.Requests;
using ServiceRequests.Application.Requests.Contracts;
using ServiceRequests.Domain.Requests;
using ServiceRequests.IntegrationTests.Infrastructure;

namespace ServiceRequests.IntegrationTests.Api;

public class BulkUpdateTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    [Fact]
    public async Task Mixed_batch_returns_200_with_outcome_per_item_in_request_order()
    {
        var valid = await TestData.AddRequestAsync(factory, status: RequestStatus.InProgress);
        var stale = await TestData.AddRequestAsync(factory, status: RequestStatus.InProgress);
        var staleVersion = RowVersionCodec.Encode(stale.RowVersion);
        await PatchStatus(stale.Id, "Waiting", staleVersion);   // someone else changed it after we loaded the list
        var notAllowed = await TestData.AddRequestAsync(factory, status: RequestStatus.New);

        var response = await Bulk("Completed", (valid.Id, Version(valid)), (999999, Version(valid)),
            (stale.Id, staleVersion), (notAllowed.Id, Version(notAllowed)));
        var result = (await response.Content.ReadFromJsonAsync<BulkUpdateResult>(Json))!;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((4, 1, 3), (result.Requested, result.Succeeded, result.Failed));
        Assert.Equal([valid.Id, 999999, stale.Id, notAllowed.Id], result.Results.Select(r => r.Id));
        Assert.Equal([BulkOutcome.Updated, BulkOutcome.NotFound, BulkOutcome.Conflict, BulkOutcome.InvalidTransition],
            result.Results.Select(r => r.Outcome));
        Assert.NotEqual(Version(valid), result.Results[0].RowVersion);   // the new version, ready for the next change
        Assert.All(result.Results.Skip(1), r => Assert.False(string.IsNullOrEmpty(r.Error)));
    }

    [Fact]
    public async Task All_failed_still_returns_200_with_zero_succeeded()
    {
        var completed = await TestData.AddRequestAsync(factory, status: RequestStatus.Completed);

        var response = await Bulk("InProgress", (completed.Id, Version(completed)), (999998, Version(completed)));
        var result = (await response.Content.ReadFromJsonAsync<BulkUpdateResult>(Json))!;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal((2, 0, 2), (result.Requested, result.Succeeded, result.Failed));
    }

    [Fact]
    public async Task Malformed_batch_returns_400_and_changes_nothing()
    {
        var request = await TestData.AddRequestAsync(factory, status: RequestStatus.InProgress);
        var before = await StateOf(request.Id);   // as stored: the column keeps milliseconds only

        // The same request twice: the whole batch is rejected, even its valid part
        var response = await Bulk("Completed", (request.Id, Version(request)), (request.Id, Version(request)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, await StateOf(request.Id));
    }

    [Fact]
    public async Task Bulk_writes_one_history_row_per_updated_request()
    {
        var requests = new List<ServiceRequest>();
        for (var i = 0; i < 3; i++)
        {
            requests.Add(await TestData.AddRequestAsync(factory, status: RequestStatus.InProgress));
        }

        await Bulk("Completed", [.. requests.Select(r => (r.Id, Version(r)))]);

        var ids = requests.Select(r => r.Id).ToList();
        List<RequestStatusHistory> rows = [];
        await factory.ExecuteDbAsync(async db => rows = await db.StatusHistory.Where(h => ids.Contains(h.RequestId)).ToListAsync());
        Assert.Equal(ids.Order(), rows.Select(h => h.RequestId).Order());
        Assert.All(rows, h => Assert.Equal((RequestStatus.InProgress, RequestStatus.Completed, "dana"), (h.PreviousStatus, h.NewStatus, h.ChangedBy)));
    }

    private static string Version(ServiceRequest request) => RowVersionCodec.Encode(request.RowVersion);

    private HttpClient Client()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Name", "dana");
        return client;
    }

    private Task<HttpResponseMessage> Bulk(string status, params (int Id, string RowVersion)[] items) =>
        Client().PostAsJsonAsync("/api/requests/bulk-status",
            new { status, items = items.Select(i => new { id = i.Id, rowVersion = i.RowVersion }) });

    private Task<HttpResponseMessage> PatchStatus(int id, string status, string rowVersion) =>
        Client().PatchAsJsonAsync($"/api/requests/{id}/status", new { status, rowVersion });

    private async Task<(RequestStatus Status, DateTime UpdatedAt)> StateOf(int id)
    {
        (RequestStatus, DateTime) state = default;
        await factory.ExecuteDbAsync(async db => state = await db.Requests.Where(r => r.Id == id)
            .Select(r => ValueTuple.Create(r.Status, r.UpdatedAt)).SingleAsync());
        return state;
    }
}
