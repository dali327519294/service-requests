using System.Collections.Frozen;
using System.Collections.ObjectModel;

namespace ServiceRequests.Domain.Requests;

/// <summary>
/// The single source of truth for which status changes are allowed.
/// Used both to enforce updates and to tell the client which transitions it may offer.
/// </summary>
public static class StatusTransitions
{
    // Read-only wrappers: a caller that casts the result back to a list cannot change the shared rules
    private static readonly FrozenDictionary<RequestStatus, ReadOnlyCollection<RequestStatus>> Allowed =
        new Dictionary<RequestStatus, ReadOnlyCollection<RequestStatus>>
        {
            [RequestStatus.New] = new([RequestStatus.InProgress]),
            [RequestStatus.InProgress] = new([RequestStatus.Waiting, RequestStatus.Completed]),
            [RequestStatus.Waiting] = new([RequestStatus.InProgress]),
            [RequestStatus.Completed] = ReadOnlyCollection<RequestStatus>.Empty, // final status
        }.ToFrozenDictionary();

    public static bool IsAllowed(RequestStatus from, RequestStatus to) => AllowedFrom(from).Contains(to);

    public static IReadOnlyList<RequestStatus> AllowedFrom(RequestStatus from) =>
        Allowed.TryGetValue(from, out var targets) ? targets : ReadOnlyCollection<RequestStatus>.Empty;
}
