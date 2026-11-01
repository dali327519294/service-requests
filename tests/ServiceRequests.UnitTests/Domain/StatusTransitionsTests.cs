using ServiceRequests.Domain.Requests;

namespace ServiceRequests.UnitTests.Domain;

public class StatusTransitionsTests
{
    [Theory]
    [InlineData(RequestStatus.New, RequestStatus.InProgress)]
    [InlineData(RequestStatus.InProgress, RequestStatus.Waiting)]
    [InlineData(RequestStatus.InProgress, RequestStatus.Completed)]
    [InlineData(RequestStatus.Waiting, RequestStatus.InProgress)]
    public void Allowed_transitions_are_allowed(RequestStatus from, RequestStatus to)
        => Assert.True(StatusTransitions.IsAllowed(from, to));

    [Theory]
    [InlineData(RequestStatus.New, RequestStatus.Completed)]
    [InlineData(RequestStatus.New, RequestStatus.Waiting)]
    [InlineData(RequestStatus.Waiting, RequestStatus.Completed)]
    [InlineData(RequestStatus.Completed, RequestStatus.InProgress)]
    [InlineData(RequestStatus.InProgress, RequestStatus.InProgress)]
    public void Other_transitions_are_rejected(RequestStatus from, RequestStatus to)
        => Assert.False(StatusTransitions.IsAllowed(from, to));

    [Fact]
    public void Completed_has_no_allowed_transitions()
        => Assert.Empty(StatusTransitions.AllowedFrom(RequestStatus.Completed));

    [Fact]
    public void Returned_list_cannot_change_the_shared_rules()
    {
        var allowed = (IList<RequestStatus>)StatusTransitions.AllowedFrom(RequestStatus.New);

        Assert.Throws<NotSupportedException>(() => allowed[0] = RequestStatus.Completed);
        Assert.False(StatusTransitions.IsAllowed(RequestStatus.New, RequestStatus.Completed));
    }

    [Fact]
    public void InProgress_allows_waiting_and_completed()
        => Assert.Equal([RequestStatus.Waiting, RequestStatus.Completed], StatusTransitions.AllowedFrom(RequestStatus.InProgress));
}
