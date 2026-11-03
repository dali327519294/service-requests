using ServiceRequests.Application.Requests.Contracts;
using ServiceRequests.Domain.Requests;

namespace ServiceRequests.Application.Requests;

/// <summary>One place that turns a request into the details the client gets.</summary>
internal static class RequestMapping
{
    public static RequestDetailsDto ToDetails(this ServiceRequest request) => new(request.Id, request.Title,
        request.OrganizationName, request.Status, request.Priority, request.AssignedTo, request.CreatedAt, request.UpdatedAt,
        RowVersionCodec.Encode(request.RowVersion), StatusTransitions.AllowedFrom(request.Status));
}
