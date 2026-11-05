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

public class UpdateStatusTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    [Fact]
    public async Task Valid_update_returns_200_with_new_rowversion_and_writes_history()
    {
        var request = await TestData.AddRequestAsync(factory, status: RequestStatus.InProgress);
        var oldVersion = RowVersionCodec.Encode(request.RowVersion);

        var response = await Patch(request.Id, new { status = "Completed", rowVersion = oldVersion });
        var details = (await response.Content.ReadFromJsonAsync<RequestDetailsDto>(Json))!;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(RequestStatus.Completed, details.Status);
        Assert.NotEqual(oldVersion, details.RowVersion);
        Assert.Empty(details.AllowedTransitions);   // Completed is final
        var row = Assert.Single(await HistoryOf(request.Id));
        Assert.Equal((RequestStatus.InProgress, RequestStatus.Completed, "dana"), (row.PreviousStatus, row.NewStatus, row.ChangedBy));
    }

    [Fact]
    public async Task Missing_request_returns_404()
    {
        var response = await Patch(999999, new { status = "InProgress", rowVersion = RowVersionCodec.Encode(new byte[8]) });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Not_allowed_transition_returns_400_with_allowed_list()
    {
        var request = await TestData.AddRequestAsync(factory, status: RequestStatus.New);

        var response = await Patch(request.Id, new { status = "Completed", rowVersion = RowVersionCodec.Encode(request.RowVersion) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(["InProgress"], json.RootElement.GetProperty("allowedTransitions").EnumerateArray().Select(s => s.GetString()));
        Assert.Empty(await HistoryOf(request.Id));
    }

    [Fact]
    public async Task Same_status_returns_400()
    {
        var request = await TestData.AddRequestAsync(factory, status: RequestStatus.New);

        var response = await Patch(request.Id, new { status = "New", rowVersion = RowVersionCodec.Encode(request.RowVersion) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("AAAAAA==")]
    public async Task Invalid_rowversion_returns_400_not_500(string rowVersion)
    {
        var request = await TestData.AddRequestAsync(factory, status: RequestStatus.New);

        var response = await Patch(request.Id, new { status = "InProgress", rowVersion });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("rowVersion", await ErrorFields(response), StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Unknown_status_number_returns_400()
    {
        var request = await TestData.AddRequestAsync(factory, status: RequestStatus.New);

        var response = await Patch(request.Id, new { status = 99, rowVersion = RowVersionCodec.Encode(request.RowVersion) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("status", await ErrorFields(response), StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Stale_rowversion_returns_409_with_current_state_before_transition_check()
    {
        var request = await TestData.AddRequestAsync(factory, status: RequestStatus.InProgress);
        var staleVersion = RowVersionCodec.Encode(request.RowVersion);
        var first = (await (await Patch(request.Id, new { status = "Waiting", rowVersion = staleVersion }))
            .Content.ReadFromJsonAsync<RequestDetailsDto>(Json))!;

        // Waiting -> Waiting is not allowed either, but the stale version must win: 409, not 400
        var response = await Patch(request.Id, new { status = "Waiting", rowVersion = staleVersion });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var current = json.RootElement.GetProperty("current");
        Assert.Equal("Waiting", current.GetProperty("status").GetString());
        Assert.Equal(first.RowVersion, current.GetProperty("rowVersion").GetString());
        Assert.Equal(["InProgress"], current.GetProperty("allowedTransitions").EnumerateArray().Select(s => s.GetString()));
    }

    [Fact]
    public async Task Missing_user_returns_400_and_changes_nothing()
    {
        var request = await TestData.AddRequestAsync(factory, status: RequestStatus.New);

        var response = await Patch(request.Id, new { status = "InProgress", rowVersion = RowVersionCodec.Encode(request.RowVersion) }, user: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await HistoryOf(request.Id));
    }

    [Fact]
    public async Task The_answer_carries_the_same_time_a_later_read_returns()
    {
        // A moment with a fraction of a millisecond, which the database cannot store
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 11, 5, 12, 0, 0, TimeSpan.Zero).AddTicks(5_806_000));
        using var api = factory.WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<TimeProvider>(clock)));
        var request = await TestData.AddRequestAsync(factory, status: RequestStatus.New);
        var client = api.CreateClient();
        client.DefaultRequestHeaders.Add("X-User-Name", "dana");

        var answer = (await (await client.PatchAsJsonAsync($"/api/requests/{request.Id}/status",
            new { status = "InProgress", rowVersion = RowVersionCodec.Encode(request.RowVersion) }))
            .Content.ReadFromJsonAsync<RequestDetailsDto>(Json))!;
        var read = (await client.GetFromJsonAsync<RequestDetailsDto>($"/api/requests/{request.Id}", Json))!;

        Assert.Equal(read.UpdatedAt, answer.UpdatedAt);
        Assert.Equal(read.UpdatedAt, Assert.Single(await HistoryOf(request.Id)).ChangedAt);
    }

    private async Task<HttpResponseMessage> Patch(int id, object body, string? user = "dana")
    {
        var client = factory.CreateClient();
        if (user is not null)
        {
            client.DefaultRequestHeaders.Add("X-User-Name", user);
        }

        return await client.PatchAsJsonAsync($"/api/requests/{id}/status", body);
    }

    private async Task<List<RequestStatusHistory>> HistoryOf(int requestId)
    {
        List<RequestStatusHistory> rows = [];
        await factory.ExecuteDbAsync(async db => rows = await db.StatusHistory.Where(h => h.RequestId == requestId).ToListAsync());
        return rows;
    }

    private static async Task<IEnumerable<string>> ErrorFields(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return [.. json.RootElement.GetProperty("errors").EnumerateObject().Select(e => e.Name)];
    }
}
