using System.ComponentModel.DataAnnotations;
using ServiceRequests.Domain.Requests;

namespace ServiceRequests.Application.Requests.Contracts;

/// <summary>One status for up to 100 requests, each with the rowVersion the client saw.</summary>
public sealed record BulkUpdateStatusRequest : IValidatableObject
{
    public const int MaxItems = 100;

    [Required]
    public RequestStatus? Status { get; init; }

    [Required]
    public IReadOnlyList<BulkItem?>? Items { get; init; }

    // A malformed batch is rejected whole: partial success is for business outcomes, not for client mistakes
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Status is { } status && !Enum.IsDefined(status))
        {
            yield return new ValidationResult("Unknown status.", [nameof(Status)]);
        }

        if (Items is null)
        {
            yield break;
        }

        if (Items.Count is 0 or > MaxItems)
        {
            yield return new ValidationResult($"Must contain 1 to {MaxItems} items.", [nameof(Items)]);
            yield break;
        }

        var seen = new HashSet<int>();
        for (var i = 0; i < Items.Count; i++)
        {
            if (Items[i] is not { } item)
            {
                yield return new ValidationResult("Must not be empty.", [$"{nameof(Items)}[{i}]"]);
                continue;
            }

            if (!seen.Add(item.Id))
            {
                yield return new ValidationResult("Appears more than once.", [$"{nameof(Items)}[{i}].{nameof(BulkItem.Id)}"]);
            }

            if (!RowVersionCodec.TryDecode(item.RowVersion, out _))
            {
                yield return new ValidationResult("Must be the rowVersion returned by the server.",
                    [$"{nameof(Items)}[{i}].{nameof(BulkItem.RowVersion)}"]);
            }
        }
    }
}

/// <summary>One request in a bulk update.</summary>
public sealed record BulkItem
{
    public int Id { get; init; }
    public string? RowVersion { get; init; }
}
