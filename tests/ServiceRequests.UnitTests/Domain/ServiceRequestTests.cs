using ServiceRequests.Domain;
using ServiceRequests.Domain.Requests;

namespace ServiceRequests.UnitTests.Domain;

public class ServiceRequestTests
{
    private static readonly DateTime CreatedAt = new(2026, 3, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Now = new(2026, 3, 2, 9, 30, 0, DateTimeKind.Utc);

    private static ServiceRequest NewRequest(RequestStatus status = RequestStatus.New) =>
        new("בקשה להיתר קרינה", "חברת דוגמה בע\"מ", RequestPriority.Medium, status, "dana", CreatedAt);

    [Fact]
    public void New_request_starts_with_UpdatedAt_equal_to_CreatedAt()
    {
        var request = NewRequest();

        Assert.Equal(RequestStatus.New, request.Status);
        Assert.Equal(CreatedAt, request.CreatedAt);
        Assert.Equal(CreatedAt, request.UpdatedAt);
    }

    [Fact]
    public void ChangeStatus_updates_status_and_UpdatedAt_and_returns_history_row()
    {
        var request = NewRequest();

        var history = request.ChangeStatus(RequestStatus.InProgress, "dana", Now);

        Assert.Equal(RequestStatus.InProgress, request.Status);
        Assert.Equal(Now, request.UpdatedAt);
        Assert.Equal(RequestStatus.New, history.PreviousStatus);
        Assert.Equal(RequestStatus.InProgress, history.NewStatus);
        Assert.Equal("dana", history.ChangedBy);
        Assert.Equal(Now, history.ChangedAt);
    }

    [Fact]
    public void ChangeStatus_to_not_allowed_status_throws_and_changes_nothing()
    {
        var request = NewRequest();

        var error = Assert.Throws<StatusTransitionNotAllowedException>(
            () => request.ChangeStatus(RequestStatus.Completed, "dana", Now));

        Assert.Equal(RequestStatus.New, error.From);
        Assert.Equal(RequestStatus.Completed, error.To);
        Assert.Equal(RequestStatus.New, request.Status);
        Assert.Equal(CreatedAt, request.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ChangeStatus_requires_who_changed_it(string changedBy)
        => Assert.Throws<ArgumentException>(() => NewRequest().ChangeStatus(RequestStatus.InProgress, changedBy, Now));

    [Fact]
    public void Title_longer_than_the_limit_is_rejected()
    {
        var title = new string('א', FieldLengths.Title + 1);

        Assert.Throws<ArgumentException>(
            () => new ServiceRequest(title, "ארגון", RequestPriority.Low, RequestStatus.New, null, CreatedAt));
    }

    [Fact]
    public void Empty_organization_name_is_rejected()
        => Assert.Throws<ArgumentException>(
            () => new ServiceRequest("כותרת", " ", RequestPriority.Low, RequestStatus.New, null, CreatedAt));
}
