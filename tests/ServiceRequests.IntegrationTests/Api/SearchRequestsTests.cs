using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using ServiceRequests.Application.Requests;
using ServiceRequests.Application.Requests.Contracts;
using ServiceRequests.Application.Requests.Querying;
using ServiceRequests.Domain.Requests;
using ServiceRequests.IntegrationTests.Infrastructure;

namespace ServiceRequests.IntegrationTests.Api;

public class SearchRequestsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    // Every test works under its own organization prefix, so tests sharing the database never see each other's rows
    private readonly string _org = $"ארגון {Guid.NewGuid():N}";

    [Fact]
    public async Task Filters_combine_with_AND()
    {
        var match = await Add(status: RequestStatus.InProgress, priority: RequestPriority.High, assignedTo: "dana.levi");
        await Add(status: RequestStatus.Waiting, priority: RequestPriority.High, assignedTo: "dana.levi");
        await Add(status: RequestStatus.InProgress, priority: RequestPriority.Low, assignedTo: "dana.levi");
        await Add(status: RequestStatus.InProgress, priority: RequestPriority.High, assignedTo: "avi.peretz");

        var page = await Search("status=InProgress&priority=High&assignedTo=dana.levi");

        Assert.Equal([match.Id], page.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task Several_values_of_one_filter_combine_with_OR()
    {
        var waiting = await Add(status: RequestStatus.Waiting);
        var completed = await Add(status: RequestStatus.Completed);
        await Add(status: RequestStatus.New);

        var page = await Search("status=Waiting&status=Completed");

        Assert.Equal([waiting.Id, completed.Id], page.Items.Select(i => i.Id).Order());
    }

    [Fact]
    public async Task Search_matches_title_or_organization_including_hebrew()
    {
        var byTitle = await Add(title: "בקשה לפינוי אסבסט");
        var byOrganization = await Add(organizationSuffix: " מפעל אסבסט");
        await Add(title: "תלונה על רעש");

        var page = await Search("search=" + Uri.EscapeDataString("אסבסט"));

        Assert.Equal([byTitle.Id, byOrganization.Id], page.Items.Select(i => i.Id).Order());
    }

    [Theory]
    [InlineData("50%", "הנחה 50% בדיוק", "הנחה 500 שקל")]
    [InlineData("a_b", "קוד a_b", "קוד axb")]
    [InlineData("[x]", "סימון [x]", "סימון x")]
    public async Task Search_treats_like_wildcards_literally(string term, string matchingTitle, string otherTitle)
    {
        var match = await Add(title: matchingTitle);
        await Add(title: otherTitle);

        var page = await Search("search=" + Uri.EscapeDataString(term));

        Assert.Equal([match.Id], page.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task Date_range_is_half_open_in_israel_time()
    {
        // 31/03 23:30 in Israel (UTC+3) belongs to that day; 01/04 00:00 is already the next day
        var lastMinutes = await Add(createdAt: new DateTime(2026, 3, 31, 20, 30, 0, DateTimeKind.Utc));
        await Add(createdAt: new DateTime(2026, 3, 31, 21, 0, 0, DateTimeKind.Utc));

        var page = await Search(
            "createdFrom=" + Uri.EscapeDataString("2026-03-31T00:00:00+03:00") +
            "&createdTo=" + Uri.EscapeDataString("2026-04-01T00:00:00+03:00"));

        Assert.Equal([lastMinutes.Id], page.Items.Select(i => i.Id));
    }

    [Theory]
    [InlineData(RequestSortField.CreatedAt, SortDirection.Asc)]
    [InlineData(RequestSortField.CreatedAt, SortDirection.Desc)]
    [InlineData(RequestSortField.UpdatedAt, SortDirection.Asc)]
    [InlineData(RequestSortField.UpdatedAt, SortDirection.Desc)]
    [InlineData(RequestSortField.Priority, SortDirection.Asc)]
    [InlineData(RequestSortField.Priority, SortDirection.Desc)]
    [InlineData(RequestSortField.Status, SortDirection.Asc)]
    [InlineData(RequestSortField.Status, SortDirection.Desc)]
    [InlineData(RequestSortField.OrganizationName, SortDirection.Asc)]
    [InlineData(RequestSortField.OrganizationName, SortDirection.Desc)]
    public async Task Sorts_by_each_whitelisted_field_in_both_directions(RequestSortField field, SortDirection direction)
    {
        // Each row is first, second or third in every field, so any wrong sort shows up
        var first = await Add(organizationSuffix: " א", status: RequestStatus.New, priority: RequestPriority.Low,
            createdAt: new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var second = await Add(organizationSuffix: " ב", status: RequestStatus.InProgress, priority: RequestPriority.Medium,
            createdAt: new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc));
        var third = await Add(organizationSuffix: " ג", status: RequestStatus.Waiting, priority: RequestPriority.High,
            createdAt: new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        int[] ascending = [first.Id, second.Id, third.Id];

        var page = await Search($"sortBy={field}&sortDirection={direction}");

        Assert.Equal(direction == SortDirection.Asc ? ascending : ascending.Reverse(), page.Items.Select(i => i.Id));
    }

    [Theory]
    [InlineData(99999)]
    [InlineData(int.MaxValue)]
    public async Task Page_past_the_end_returns_empty_items_with_total(int pageNumber)
    {
        for (var i = 0; i < 3; i++)
        {
            await Add();
        }

        var page = await Search($"page={pageNumber}&pageSize=2");

        Assert.Empty(page.Items);
        Assert.Equal(3, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
    }

    [Fact]
    public async Task Pages_do_not_overlap()
    {
        for (var i = 0; i < 3; i++)
        {
            await Add();
        }

        var firstPage = await Search("page=1&pageSize=2");
        var secondPage = await Search("page=2&pageSize=2");

        Assert.Equal(3, firstPage.Items.Concat(secondPage.Items).Select(i => i.Id).Distinct().Count());
    }

    [Theory]
    [InlineData("pageSize=500", "PageSize")]
    [InlineData("sortBy=password", "SortBy")]
    [InlineData("status=99", "Status")]
    [InlineData("createdFrom=2026-04-02T00:00:00Z&createdTo=2026-04-01T00:00:00Z", "CreatedFrom")]
    public async Task Invalid_parameters_return_400_with_field_names(string queryString, string field)
    {
        var response = await factory.CreateClient().GetAsync("/api/requests?" + queryString);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains(json.RootElement.GetProperty("errors").EnumerateObject(),
            error => error.Name.Equals(field, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("createdFrom=2026-04-01T00:00:00", "CreatedFrom")]
    [InlineData("createdTo=2026-04-01", "CreatedTo")]
    public async Task Date_without_an_offset_returns_400(string queryString, string field)
    {
        var response = await factory.CreateClient().GetAsync("/api/requests?" + queryString);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Contains(json.RootElement.GetProperty("errors").EnumerateObject(),
            error => error.Name.Equals(field, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Get_by_id_returns_details_with_rowversion_allowed_transitions_and_utc_dates()
    {
        var request = await Add(status: RequestStatus.InProgress);

        var response = await factory.CreateClient().GetAsync($"/api/requests/{request.Id}");
        var body = await response.Content.ReadAsStringAsync();
        var details = JsonSerializer.Deserialize<RequestDetailsDto>(body, Json)!;

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"status\":\"InProgress\"", body, StringComparison.Ordinal);
        Assert.Equal([RequestStatus.Waiting, RequestStatus.Completed], details.AllowedTransitions);
        Assert.True(RowVersionCodec.TryDecode(details.RowVersion, out var rowVersion));
        Assert.Equal(request.RowVersion, rowVersion);
        using var json = JsonDocument.Parse(body);
        Assert.EndsWith("Z", json.RootElement.GetProperty("createdAt").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_missing_id_returns_404_problem_details()
    {
        var response = await factory.CreateClient().GetAsync("/api/requests/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Search_honours_a_cancelled_token()
    {
        using var scope = factory.Services.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<RequestQueryService>();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => queries.SearchAsync(new RequestSearchQuery(), new CancellationToken(canceled: true)));
    }

    [Fact]
    public async Task A_rare_search_reads_the_table_once_and_still_pages_newest_first()
    {
        var term = $"נדיר{Guid.NewGuid():N}"[..12];
        var start = new DateTime(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);
        var matches = new List<ServiceRequest>();
        for (var i = 0; i < 3; i++)
        {
            matches.Add(await Add(title: $"בקשה {term} {i}", createdAt: start.AddDays(i)));
        }
        await Add(title: "בקשה אחרת", createdAt: start.AddDays(10));

        // The recorder is shared by the whole class: only the commands of this search count
        var before = factory.Sql.Commands.Count;
        var first = await Search($"search={Uri.EscapeDataString(term)}&pageSize=2");
        var firstPageSql = factory.Sql.Commands.Skip(before).ToList();
        var second = await Search($"search={Uri.EscapeDataString(term)}&pageSize=2&page=2");

        Assert.Equal(3, first.TotalCount);
        Assert.Equal([matches[2].Id, matches[1].Id], first.Items.Select(i => i.Id));
        Assert.Equal([matches[0].Id], second.Items.Select(i => i.Id));
        // With so few matches, walking the date index would check nearly every row (docs/PERFORMANCE.md)
        Assert.Contains(firstPageSql, sql => sql.Contains("WITH (INDEX(1))", StringComparison.Ordinal));
    }

    private Task<ServiceRequest> Add(string title = "Test request", string organizationSuffix = "",
        RequestStatus status = RequestStatus.New, RequestPriority priority = RequestPriority.Medium,
        string? assignedTo = null, DateTime? createdAt = null) =>
        TestData.AddRequestAsync(factory, title, _org + organizationSuffix, status, priority, assignedTo, createdAt);

    // Every search is scoped to this test's organization prefix
    private async Task<PagedResult<RequestListItemDto>> Search(string queryString)
    {
        var url = "/api/requests?organizationName=" + Uri.EscapeDataString(_org) + "&" + queryString;
        return (await factory.CreateClient().GetFromJsonAsync<PagedResult<RequestListItemDto>>(url, Json))!;
    }
}
