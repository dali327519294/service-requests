using System.ComponentModel.DataAnnotations;
using ServiceRequests.Application.Requests.Contracts;
using ServiceRequests.Domain.Requests;

namespace ServiceRequests.UnitTests.Application;

public class RequestSearchQueryValidationTests
{
    private static List<string> InvalidMembers(RequestSearchQuery query)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(query, new ValidationContext(query), results, validateAllProperties: true);
        return [.. results.SelectMany(r => r.MemberNames)];
    }

    [Fact]
    public void Default_query_is_valid() => Assert.Empty(InvalidMembers(new RequestSearchQuery()));

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void PageSize_outside_1_to_100_is_invalid(int pageSize) =>
        Assert.Contains(nameof(RequestSearchQuery.PageSize), InvalidMembers(new RequestSearchQuery { PageSize = pageSize }));

    [Fact]
    public void Page_below_1_is_invalid() =>
        Assert.Contains(nameof(RequestSearchQuery.Page), InvalidMembers(new RequestSearchQuery { Page = 0 }));

    [Theory]
    [InlineData(nameof(RequestSearchQuery.Search), 101)]
    [InlineData(nameof(RequestSearchQuery.OrganizationName), 201)]
    [InlineData(nameof(RequestSearchQuery.AssignedTo), 101)]
    public void Text_longer_than_its_limit_is_invalid(string field, int length)
    {
        var text = new string('א', length);
        var query = field switch
        {
            nameof(RequestSearchQuery.Search) => new RequestSearchQuery { Search = text },
            nameof(RequestSearchQuery.OrganizationName) => new RequestSearchQuery { OrganizationName = text },
            _ => new RequestSearchQuery { AssignedTo = text },
        };

        Assert.Contains(field, InvalidMembers(query));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-60)]
    public void CreatedFrom_not_earlier_than_CreatedTo_is_invalid(int toMinutesAfterFrom)
    {
        var from = new DateTimeOffset(2026, 4, 1, 0, 0, 0, TimeSpan.FromHours(3));

        var invalid = InvalidMembers(new RequestSearchQuery { CreatedFrom = from, CreatedTo = from.AddMinutes(toMinutesAfterFrom) });

        Assert.Contains(nameof(RequestSearchQuery.CreatedFrom), invalid);
    }

    [Fact]
    public void Undefined_enum_values_are_invalid()
    {
        var invalid = InvalidMembers(new RequestSearchQuery
        {
            Status = [(RequestStatus)99],
            Priority = [(RequestPriority)0],
            SortBy = (RequestSortField)42,
            SortDirection = (SortDirection)7,
        });

        Assert.Equal(
            [nameof(RequestSearchQuery.Status), nameof(RequestSearchQuery.Priority), nameof(RequestSearchQuery.SortBy), nameof(RequestSearchQuery.SortDirection)],
            invalid);
    }
}
