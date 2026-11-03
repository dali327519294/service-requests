using ServiceRequests.Application.Requests.Contracts;
using ServiceRequests.Domain.Requests;

namespace ServiceRequests.Application.Requests.Commands;

/// <summary>Every way a status update can end. Expected business results, not exceptions; the API maps each to a status code.</summary>
public abstract record UpdateStatusOutcome
{
    // Private: the list of outcomes below is closed
    private UpdateStatusOutcome()
    {
    }

    public sealed record Updated(RequestDetailsDto Request, RequestStatus PreviousStatus) : UpdateStatusOutcome;

    public sealed record NotFound : UpdateStatusOutcome;

    public sealed record Conflict(RequestDetailsDto Current) : UpdateStatusOutcome;

    public sealed record InvalidTransition(RequestStatus From, RequestStatus To, IReadOnlyList<RequestStatus> Allowed)
        : UpdateStatusOutcome;
}
