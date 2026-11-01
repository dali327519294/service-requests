namespace ServiceRequests.Domain.Requests;

public class ServiceRequest
{
    public int Id { get; private set; }
    public string Title { get; private set; }
    public string OrganizationName { get; private set; }
    public RequestStatus Status { get; private set; }
    public RequestPriority Priority { get; private set; }
    public string? AssignedTo { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>Set by SQL Server on every update; the optimistic concurrency token.</summary>
    public byte[] RowVersion { get; private set; } = [];

    public ServiceRequest(string title, string organizationName, RequestPriority priority, RequestStatus status, string? assignedTo, DateTime createdAt)
    {
        Title = RequireText(title, FieldLengths.Title, nameof(title));
        OrganizationName = RequireText(organizationName, FieldLengths.OrganizationName, nameof(organizationName));
        Priority = priority;
        Status = status;
        AssignedTo = assignedTo;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    /// <summary>Changes the status if the transition is allowed, and returns the audit row to save with it.</summary>
    /// <exception cref="StatusTransitionNotAllowedException">The transition is not allowed from the current status.</exception>
    public RequestStatusHistory ChangeStatus(RequestStatus newStatus, string changedBy, DateTime utcNow)
    {
        RequireText(changedBy, FieldLengths.ChangedBy, nameof(changedBy));

        if (!StatusTransitions.IsAllowed(Status, newStatus))
        {
            throw new StatusTransitionNotAllowedException(Status, newStatus);
        }

        var history = new RequestStatusHistory(Id, Status, newStatus, utcNow, changedBy);
        Status = newStatus;
        UpdatedAt = utcNow;
        return history;
    }

    private static string RequireText(string value, int maxLength, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);
        if (value.Length > maxLength)
        {
            throw new ArgumentException($"Must be at most {maxLength} characters.", paramName);
        }

        return value;
    }
}
