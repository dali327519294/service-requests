using System.ComponentModel.DataAnnotations;
using ServiceRequests.Domain;
using ServiceRequests.Domain.Requests;

namespace ServiceRequests.Application.Requests.Contracts;

/// <summary>Search, filter, sort and paging parameters for the request list. All filters combine with AND.</summary>
public sealed record RequestSearchQuery : IValidatableObject
{
    public const int MaxPageSize = 100;

    /// <summary>Text contained in the title or the organization name.</summary>
    [StringLength(FieldLengths.Search)]
    public string? Search { get; init; }

    public IReadOnlyList<RequestStatus>? Status { get; init; }
    public IReadOnlyList<RequestPriority>? Priority { get; init; }

    /// <summary>The organization name starts with this text.</summary>
    [StringLength(FieldLengths.OrganizationName)]
    public string? OrganizationName { get; init; }

    /// <summary>Exact user name of the handler.</summary>
    [StringLength(FieldLengths.AssignedTo)]
    public string? AssignedTo { get; init; }

    /// <summary>Inclusive start of the creation range, with an offset; compared in UTC.</summary>
    public DateTimeOffset? CreatedFrom { get; init; }

    /// <summary>Exclusive end, so "until midnight" never includes the first minutes of the next day.</summary>
    public DateTimeOffset? CreatedTo { get; init; }

    public RequestSortField SortBy { get; init; } = RequestSortField.CreatedAt;
    public SortDirection SortDirection { get; init; } = SortDirection.Desc;

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, MaxPageSize)]
    public int PageSize { get; init; } = 20;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (CreatedFrom >= CreatedTo)
        {
            yield return new ValidationResult("createdFrom must be earlier than createdTo.", [nameof(CreatedFrom), nameof(CreatedTo)]);
        }

        // Model binding accepts any number for an enum ("status=99"), so undefined values are rejected here
        if (Status?.Any(status => !Enum.IsDefined(status)) == true)
        {
            yield return new ValidationResult("Unknown status.", [nameof(Status)]);
        }

        if (Priority?.Any(priority => !Enum.IsDefined(priority)) == true)
        {
            yield return new ValidationResult("Unknown priority.", [nameof(Priority)]);
        }

        if (!Enum.IsDefined(SortBy))
        {
            yield return new ValidationResult("Unknown sort field.", [nameof(SortBy)]);
        }

        if (!Enum.IsDefined(SortDirection))
        {
            yield return new ValidationResult("Unknown sort direction.", [nameof(SortDirection)]);
        }
    }
}
