using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceRequests.Application.Requests;
using ServiceRequests.Application.Requests.Contracts;
using ServiceRequests.Domain.Requests;
using ServiceRequests.IntegrationTests.Infrastructure;

namespace ServiceRequests.IntegrationTests.Api;

public class BulkConcurrencyTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    [Fact]
    public async Task Row_changed_between_read_and_save_fails_only_that_row()
    {
        var requests = await AddInProgress(3);
        // Another user moves the middle one just before the batch is saved
        factory.Services.GetRequiredService<RaceInjector>().Arm(requests[1].Id, RequestStatus.Waiting);

        var result = await Bulk(requests);

        Assert.Equal([BulkOutcome.Updated, BulkOutcome.Conflict, BulkOutcome.Updated], result.Results.Select(r => r.Outcome));
        var state = await StateOf(requests);
        Assert.Equal([RequestStatus.Completed, RequestStatus.Waiting, RequestStatus.Completed], state.Select(s => s.Status));
        Assert.Equal([1, 0, 1], state.Select(s => s.HistoryRows));   // the conflicting row left no history behind
    }

    [Fact]
    public async Task Conflict_at_the_end_of_a_full_batch_still_saves_the_other_99()
    {
        var requests = await AddInProgress(BulkUpdateStatusRequest.MaxItems);
        // The conflict lands in the last command batch, after earlier batches already ran and were rolled back
        factory.Services.GetRequiredService<RaceInjector>().Arm(requests[^1].Id, RequestStatus.Waiting);

        var result = await Bulk(requests);

        Assert.Equal((100, 99, 1), (result.Requested, result.Succeeded, result.Failed));
        Assert.Equal(BulkOutcome.Conflict, result.Results[^1].Outcome);
        var state = await StateOf(requests);
        Assert.Equal(99, state.Count(s => s == (RequestStatus.Completed, 1)));
        Assert.Equal((RequestStatus.Waiting, 0), state[^1]);
    }

    [Fact]
    public async Task Bulk_and_single_updates_on_same_requests_change_each_exactly_once()
    {
        var requests = await AddInProgress(10);

        // The batch and ten single updates race for the same ten requests, all from the same version
        var bulkTask = Bulk(requests);
        var singleTasks = requests.Select(r => Client().PatchAsJsonAsync($"/api/requests/{r.Id}/status",
            new { status = "Completed", rowVersion = RowVersionCodec.Encode(r.RowVersion) })).ToList();
        var singles = await Task.WhenAll(singleTasks);
        var bulk = await bulkTask;

        var singleWins = singles.Count(s => s.StatusCode == HttpStatusCode.OK);
        Assert.All(singles, s => Assert.Contains(s.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict }));
        Assert.Equal(requests.Count, bulk.Succeeded + singleWins);
        Assert.All(await StateOf(requests), s => Assert.Equal((RequestStatus.Completed, 1), s));
    }

    private async Task<List<ServiceRequest>> AddInProgress(int count)
    {
        var requests = new List<ServiceRequest>();
        for (var i = 0; i < count; i++)
        {
            requests.Add(await TestData.AddRequestAsync(factory, status: RequestStatus.InProgress));
        }

        return requests;
    }

    private HttpClient Client()
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Name", "dana");
        return client;
    }

    private async Task<BulkUpdateResult> Bulk(List<ServiceRequest> requests)
    {
        var response = await Client().PostAsJsonAsync("/api/requests/bulk-status", new
        {
            status = "Completed",
            items = requests.Select(r => new { id = r.Id, rowVersion = RowVersionCodec.Encode(r.RowVersion) }),
        });
        return (await response.Content.ReadFromJsonAsync<BulkUpdateResult>(Json))!;
    }

    private async Task<List<(RequestStatus Status, int HistoryRows)>> StateOf(List<ServiceRequest> requests)
    {
        var state = new List<(RequestStatus, int)>();
        await factory.ExecuteDbAsync(async db =>
        {
            foreach (var request in requests)
            {
                state.Add((await db.Requests.Where(r => r.Id == request.Id).Select(r => r.Status).SingleAsync(),
                    await db.StatusHistory.CountAsync(h => h.RequestId == request.Id)));
            }
        });
        return state;
    }
}
