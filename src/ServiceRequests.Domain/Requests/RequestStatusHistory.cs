namespace ServiceRequests.Domain.Requests;

/// <summary>One audit row per status change. Rows are only ever added, never updated or deleted.</summary>
public class RequestStatusHistory
{
    public long Id { get; private set; }
    public int RequestId { get; private set; }
    public RequestStatus PreviousStatus { get; private set; }
    public RequestStatus NewStatus { get; private set; }
    public DateTime ChangedAt { get; private set; }
    public string ChangedBy { get; private set; }

    public RequestStatusHistory(int requestId, RequestStatus previousStatus, RequestStatus newStatus, DateTime changedAt, string changedBy)
    {
        RequestId = requestId;
        PreviousStatus = previousStatus;
        NewStatus = newStatus;
        ChangedAt = changedAt;
        ChangedBy = changedBy;
    }
}
