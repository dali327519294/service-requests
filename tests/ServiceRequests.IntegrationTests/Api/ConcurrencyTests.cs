using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceRequests.Application.Requests;
using ServiceRequests.Domain.Requests;
using ServiceRequests.IntegrationTests.Infrastructure;

namespace ServiceRequests.IntegrationTests.Api;

public class ConcurrencyTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Change_between_read_and_save_returns_409_and_overwrites_nothing()
    {
        var request = await TestData.AddRequestAsync(factory, status: RequestStatus.InProgress);
        // Another user moves it to Waiting after our read, just before our write
        factory.Services.GetRequiredService<RaceInjector>().Arm(request.Id, RequestStatus.Waiting);

        var response = await Patch(request.Id, "Completed", RowVersionCodec.Encode(request.RowVersion));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Waiting", json.RootElement.GetProperty("current").GetProperty("status").GetString());
        var (status, historyRows) = await StateOf(request.Id);
        Assert.Equal(RequestStatus.Waiting, status);   // the other user's change survived
        Assert.Equal(0, historyRows);                  // our history row was rolled back with our update
    }

    [Fact]
    public async Task Parallel_updates_of_different_requests_never_conflict()
    {
        var requests = new List<ServiceRequest>();
        for (var i = 0; i < 20; i++)
        {
            requests.Add(await TestData.AddRequestAsync(factory, status: RequestStatus.InProgress));
        }

        var responses = await Task.WhenAll(requests.Select(r => Patch(r.Id, "Completed", RowVersionCodec.Encode(r.RowVersion))));

        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));
    }

    [Fact]
    public async Task Parallel_updates_of_same_request_have_exactly_one_winner()
    {
        var request = await TestData.AddRequestAsync(factory, status: RequestStatus.InProgress);
        var version = RowVersionCodec.Encode(request.RowVersion);

        var responses = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => Patch(request.Id, "Completed", version)));

        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.Equal(9, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(1, (await StateOf(request.Id)).HistoryRows);
    }

    private Task<HttpResponseMessage> Patch(int id, string status, string rowVersion)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Name", "dana");
        return client.PatchAsJsonAsync($"/api/requests/{id}/status", new { status, rowVersion });
    }

    private async Task<(RequestStatus Status, int HistoryRows)> StateOf(int requestId)
    {
        (RequestStatus, int) state = default;
        await factory.ExecuteDbAsync(async db => state = (
            await db.Requests.Where(r => r.Id == requestId).Select(r => r.Status).SingleAsync(),
            await db.StatusHistory.CountAsync(h => h.RequestId == requestId)));
        return state;
    }
}
