namespace ServiceRequests.Domain.Requests;

public sealed class StatusTransitionNotAllowedException(RequestStatus from, RequestStatus to)
    : InvalidOperationException($"Cannot change status from {from} to {to}.")
{
    public RequestStatus From { get; } = from;
    public RequestStatus To { get; } = to;
}
