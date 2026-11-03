using System.ComponentModel.DataAnnotations;
using ServiceRequests.Domain.Requests;

namespace ServiceRequests.Application.Requests.Contracts;

/// <summary>The body of a status update. RowVersion is the version the client saw; a stale one means 409.</summary>
public sealed record UpdateStatusRequest : IValidatableObject
{
    [Required]
    public RequestStatus? Status { get; init; }

    [Required]
    public string? RowVersion { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // JSON accepts any number for an enum, so 99 has to be rejected here
        if (Status is { } status && !Enum.IsDefined(status))
        {
            yield return new ValidationResult("Unknown status.", [nameof(Status)]);
        }

        if (RowVersion is not null && !RowVersionCodec.TryDecode(RowVersion, out _))
        {
            yield return new ValidationResult("Must be the rowVersion returned by the server.", [nameof(RowVersion)]);
        }
    }
}
