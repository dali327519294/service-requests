using System.Collections.Frozen;

namespace ServiceRequests.Domain.Requests;

/// <summary>
/// The single source of truth for which status changes are allowed.
/// Used both to enforce updates and to tell the client which transitions it may offer.
/// </summary>
public static class StatusTransitions
{
    private static readonly FrozenDictionary<RequestStatus, RequestStatus[]> Allowed =
        new Dictionary<RequestStatus, RequestStatus[]>
        {
            [RequestStatus.New] = [RequestStatus.InProgress],
            [RequestStatus.InProgress] = [RequestStatus.Waiting, RequestStatus.Completed],
            [RequestStatus.Waiting] = [RequestStatus.InProgress],
            [RequestStatus.Completed] = [], // final status
        }.ToFrozenDictionary();

    public static bool IsAllowed(RequestStatus from, RequestStatus to) => AllowedFrom(from).Contains(to);

    public static IReadOnlyList<RequestStatus> AllowedFrom(RequestStatus from) =>
        Allowed.TryGetValue(from, out var targets) ? targets : [];
}
