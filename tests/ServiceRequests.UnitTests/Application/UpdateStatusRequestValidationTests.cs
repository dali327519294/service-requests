using System.ComponentModel.DataAnnotations;
using ServiceRequests.Application.Requests.Contracts;
using ServiceRequests.Domain.Requests;

namespace ServiceRequests.UnitTests.Application;

public class UpdateStatusRequestValidationTests
{
    private const string EightBytes = "AAAAAAAAB9E=";

    private static List<string> InvalidMembers(UpdateStatusRequest request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return [.. results.SelectMany(r => r.MemberNames)];
    }

    [Fact]
    public void Valid_request_passes() =>
        Assert.Empty(InvalidMembers(new UpdateStatusRequest { Status = RequestStatus.Completed, RowVersion = EightBytes }));

    [Fact]
    public void Missing_values_are_invalid() =>
        Assert.Equal([nameof(UpdateStatusRequest.Status), nameof(UpdateStatusRequest.RowVersion)],
            InvalidMembers(new UpdateStatusRequest()));

    [Fact]
    public void Undefined_status_is_invalid() =>
        Assert.Equal([nameof(UpdateStatusRequest.Status)],
            InvalidMembers(new UpdateStatusRequest { Status = (RequestStatus)99, RowVersion = EightBytes }));

    [Theory]
    [InlineData("abc")]        // not Base64
    [InlineData("AAAAAA==")]   // 4 bytes
    public void RowVersion_that_is_not_8_bytes_is_invalid(string rowVersion) =>
        Assert.Equal([nameof(UpdateStatusRequest.RowVersion)],
            InvalidMembers(new UpdateStatusRequest { Status = RequestStatus.Completed, RowVersion = rowVersion }));
}
