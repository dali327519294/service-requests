using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using ServiceRequests.Application.Requests.Contracts;
using ServiceRequests.Domain.Requests;
using ServiceRequests.IntegrationTests.Infrastructure;

namespace ServiceRequests.IntegrationTests.Api;

public class SummaryTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    // Every test works under its own organization prefix, so tests sharing the database never see each other's rows
    private readonly string _org = $"ארגון {Guid.NewGuid():N}";

    [Fact]
    public async Task Summary_counts_are_faceted()
    {
        await Add(RequestStatus.New, RequestPriority.High);
        await Add(RequestStatus.New, RequestPriority.Low);
        await Add(RequestStatus.InProgress, RequestPriority.High);
        await Add(RequestStatus.Waiting, RequestPriority.Medium);
        var filters = "organizationName=" + Uri.EscapeDataString(_org) + "&status=New";
        var client = factory.CreateClient();

        var body = await client.GetStringAsync("/api/requests/summary?" + filters);
        var summary = JsonSerializer.Deserialize<RequestSummaryDto>(body, Json)!;
        var list = (await client.GetFromJsonAsync<PagedResult<RequestListItemDto>>("/api/requests?" + filters, Json))!;

        // Status counts ignore the status filter, so each status shows what choosing it would return
        Assert.Equal([2, 1, 1, 0], Enum.GetValues<RequestStatus>().Select(s => summary.ByStatus[s]));
        // Priority counts keep the status filter: only the two New requests
        Assert.Equal([1, 0, 1], Enum.GetValues<RequestPriority>().Select(p => summary.ByPriority[p]));
        Assert.Equal(2, summary.Total);
        Assert.Equal(list.TotalCount, summary.Total);
        // Every value is present, zero included, and keys are names
        Assert.Contains("\"Completed\":0", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Invalid_filters_return_400()
    {
        var response = await factory.CreateClient().GetAsync("/api/requests/summary?status=99");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private Task<ServiceRequest> Add(RequestStatus status, RequestPriority priority) =>
        TestData.AddRequestAsync(factory, organizationName: _org, status: status, priority: priority);
}
