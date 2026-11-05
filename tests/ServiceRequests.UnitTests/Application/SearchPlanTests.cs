using ServiceRequests.Application.Requests.Querying;

namespace ServiceRequests.UnitTests.Application;

public class SearchPlanTests
{
    [Theory]
    // First page of 20: the table is read once below 40 matches
    [InlineData("אסבסט", 39, 0, 20, true)]
    [InlineData("אסבסט", 40, 0, 20, false)]
    [InlineData("אסבסט", 0, 0, 20, true)]
    // A deeper page needs more matches before walking the date index pays off
    [InlineData("אסבסט", 199, 80, 20, true)]
    [InlineData("אסבסט", 200, 80, 20, false)]
    public void Reads_the_table_once_when_a_search_has_few_matches(string search, int totalCount, long skip, int pageSize,
        bool expected) =>
        Assert.Equal(expected, SearchPlan.ScansTheTable(search, totalCount, skip, pageSize));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    // Without a text search SQL Server estimates the filters well and picks the right index itself
    public void Never_reads_the_whole_table_without_a_text_search(string? search) =>
        Assert.False(SearchPlan.ScansTheTable(search, 5, 0, 20));
}
