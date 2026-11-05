namespace ServiceRequests.Application.Requests.Querying;

/// <summary>How a page of search results is read (docs/PERFORMANCE.md).</summary>
public static class SearchPlan
{
    /// <summary>
    /// A text search (LIKE '%term%') cannot be estimated, so SQL Server walks the date index and checks row after
    /// row until the page is full. With few matches for the rows the page needs, that is nearly every row, one lookup
    /// each: 298,776 reads and 344 ms of CPU at one match per row needed. Reading the table once costs 1,881 reads and
    /// about 200 ms of CPU, whatever the term. Measured on 100,000 requests: below 2 matches per row needed it wins
    /// on both. Above that the date index costs less CPU, and its extra reads are repeated visits to pages already
    /// in memory (the whole table is 1,879 pages), so it stays.
    /// </summary>
    public const int ScanBelowMatchesPerRowShown = 2;

    public static bool ScansTheTable(string? search, int totalCount, long skip, int pageSize) =>
        !string.IsNullOrWhiteSpace(search) && totalCount < ScanBelowMatchesPerRowShown * (skip + pageSize);
}
