namespace ServiceRequests.Domain.Requests;

// Explicit values: stored as tinyint, so reordering the enum never changes existing data
public enum RequestStatus : byte
{
    New = 1,
    InProgress = 2,
    Waiting = 3,
    Completed = 4,
}
