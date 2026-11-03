using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ServiceRequests.Application.Requests;
using ServiceRequests.Application.Requests.Contracts;
using ServiceRequests.Domain.Requests;
using ServiceRequests.IntegrationTests.Infrastructure;

namespace ServiceRequests.IntegrationTests.Api;

public class HistoryTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    [Fact]
    public async Task History_is_newest_first()
    {
        var request = await TestData.AddRequestAsync(factory, status: RequestStatus.New);
        var version = RowVersionCodec.Encode(request.RowVersion);
        foreach (var status in new[] { "InProgress", "Waiting", "InProgress" })
        {
            version = (await (await Patch(request.Id, status, version)).Content.ReadFromJsonAsync<RequestDetailsDto>(Json))!.RowVersion;
        }

        var history = (await factory.CreateClient().GetFromJsonAsync<List<StatusHistoryDto>>($"/api/requests/{request.Id}/history", Json))!;

        Assert.Equal([RequestStatus.Waiting, RequestStatus.InProgress, RequestStatus.New], history.Select(h => h.PreviousStatus));
        Assert.All(history, h => Assert.Equal("dana", h.ChangedBy));
    }

    [Fact]
    public async Task History_of_unchanged_request_is_empty()
    {
        var request = await TestData.AddRequestAsync(factory);

        var history = await factory.CreateClient().GetFromJsonAsync<List<StatusHistoryDto>>($"/api/requests/{request.Id}/history", Json);

        Assert.Empty(history!);
    }

    [Fact]
    public async Task History_of_missing_request_returns_404()
    {
        var response = await factory.CreateClient().GetAsync("/api/requests/999999/history");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private Task<HttpResponseMessage> Patch(int id, string status, string rowVersion)
    {
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Name", "dana");
        return client.PatchAsJsonAsync($"/api/requests/{id}/status", new { status, rowVersion });
    }
}
