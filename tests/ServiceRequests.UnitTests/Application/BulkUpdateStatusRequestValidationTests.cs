using System.ComponentModel.DataAnnotations;
using ServiceRequests.Application.Requests.Contracts;
using ServiceRequests.Domain.Requests;

namespace ServiceRequests.UnitTests.Application;

public class BulkUpdateStatusRequestValidationTests
{
    private const string EightBytes = "AAAAAAAAB9E=";

    private static List<string> InvalidMembers(BulkUpdateStatusRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return [.. results.SelectMany(r => r.MemberNames)];
    }

    private static BulkUpdateStatusRequest Bulk(params BulkItem?[] items) =>
        new() { Status = RequestStatus.Completed, Items = items };

    private static BulkItem Item(int id, string? rowVersion = EightBytes) => new() { Id = id, RowVersion = rowVersion };

    [Fact]
    public void Hundred_items_pass() => Assert.Empty(InvalidMembers(Bulk([.. Enumerable.Range(1, 100).Select(i => Item(i))])));

    [Fact]
    public void Missing_status_and_items_are_invalid() =>
        Assert.Equal([nameof(BulkUpdateStatusRequest.Status), nameof(BulkUpdateStatusRequest.Items)],
            InvalidMembers(new BulkUpdateStatusRequest()));

    [Fact]
    public void Undefined_status_is_invalid() =>
        Assert.Contains(nameof(BulkUpdateStatusRequest.Status),
            InvalidMembers(new BulkUpdateStatusRequest { Status = (RequestStatus)99, Items = [Item(1)] }));

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Item_count_outside_1_to_100_is_invalid(int count) =>
        Assert.Contains(nameof(BulkUpdateStatusRequest.Items), InvalidMembers(Bulk([.. Enumerable.Range(1, count).Select(i => Item(i))])));

    [Fact]
    public void Null_item_is_invalid() => Assert.Contains("Items[1]", InvalidMembers(Bulk(Item(1), null)));

    [Fact]
    public void Duplicate_id_is_invalid() => Assert.Contains("Items[1].Id", InvalidMembers(Bulk(Item(7), Item(7))));

    [Theory]
    [InlineData(null)]
    [InlineData("abc")]
    [InlineData("AAAAAA==")]
    public void Missing_or_malformed_rowversion_is_invalid(string? rowVersion) =>
        Assert.Contains("Items[1].RowVersion", InvalidMembers(Bulk(Item(1), Item(2, rowVersion))));
}
