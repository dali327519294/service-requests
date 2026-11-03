using ServiceRequests.Application.Requests.Contracts;
using ServiceRequests.Application.Requests.Querying;
using ServiceRequests.Domain.Requests;

namespace ServiceRequests.UnitTests.Application;

public class RequestFiltersTests
{
    [Fact]
    public void Default_query_has_no_filters() => Assert.True(RequestFilters.IsEmpty(new RequestSearchQuery()));

    [Fact]
    public void Paging_sorting_and_blank_values_are_not_filters() =>
        Assert.True(RequestFilters.IsEmpty(new RequestSearchQuery
        {
            Page = 5, PageSize = 50, SortBy = RequestSortField.Priority, SortDirection = SortDirection.Asc,
            Search = "  ", OrganizationName = "", AssignedTo = " ", Status = [], Priority = [],
        }));

    [Theory]
    [InlineData(nameof(RequestSearchQuery.Search))]
    [InlineData(nameof(RequestSearchQuery.Status))]
    [InlineData(nameof(RequestSearchQuery.Priority))]
    [InlineData(nameof(RequestSearchQuery.OrganizationName))]
    [InlineData(nameof(RequestSearchQuery.AssignedTo))]
    [InlineData(nameof(RequestSearchQuery.CreatedFrom))]
    [InlineData(nameof(RequestSearchQuery.CreatedTo))]
    public void Any_single_filter_makes_the_query_filtered(string filter)
    {
        var date = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.FromHours(3));
        var query = filter switch
        {
            nameof(RequestSearchQuery.Search) => new RequestSearchQuery { Search = "אסבסט" },
            nameof(RequestSearchQuery.Status) => new RequestSearchQuery { Status = [RequestStatus.New] },
            nameof(RequestSearchQuery.Priority) => new RequestSearchQuery { Priority = [RequestPriority.High] },
            nameof(RequestSearchQuery.OrganizationName) => new RequestSearchQuery { OrganizationName = "מפעל" },
            nameof(RequestSearchQuery.AssignedTo) => new RequestSearchQuery { AssignedTo = "dana.levi" },
            nameof(RequestSearchQuery.CreatedFrom) => new RequestSearchQuery { CreatedFrom = date },
            _ => new RequestSearchQuery { CreatedTo = date },
        };

        Assert.False(RequestFilters.IsEmpty(query));
    }
}
