using System.Collections.Frozen;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using ServiceRequests.Domain.Requests;

namespace ServiceRequests.Infrastructure.Persistence.Seeding;

/// <summary>Replaces all requests and their history with the same generated data on every run.</summary>
public sealed partial class DataSeeder(ServiceRequestsDbContext db, TimeProvider timeProvider, ILogger<DataSeeder> logger)
{
    public const int DefaultCount = 100_000;
    private const int RandomSeed = 20261101;
    private const int BatchSize = 10_000;

    private static readonly (RequestStatus Value, int Weight)[] StatusWeights =
        [(RequestStatus.New, 10), (RequestStatus.InProgress, 40), (RequestStatus.Waiting, 15), (RequestStatus.Completed, 35)];

    private static readonly (RequestPriority Value, int Weight)[] PriorityWeights =
        [(RequestPriority.Low, 30), (RequestPriority.Medium, 50), (RequestPriority.High, 20)];

    // For each status, the shortest chain of allowed transitions that leads to it from New
    private static readonly FrozenDictionary<RequestStatus, RequestStatus[]> PathFromNew =
        Enum.GetValues<RequestStatus>().ToFrozenDictionary(status => status, ShortestPathFromNew);

    public async Task SeedAsync(int requestCount, CancellationToken ct)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(requestCount);
        var stopwatch = Stopwatch.StartNew();
        var (requests, history) = Generate(requestCount);
        using (requests)
        using (history)
        {
            // One transaction: a failure halfway leaves the previous data untouched
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await db.Database.ExecuteSqlRawAsync("TRUNCATE TABLE RequestStatusHistory; DELETE FROM Requests;", ct);

            var connection = (SqlConnection)db.Database.GetDbConnection();
            var sqlTransaction = (SqlTransaction)transaction.GetDbTransaction();
            await BulkInsertAsync(connection, sqlTransaction, "Requests", requests, SqlBulkCopyOptions.KeepIdentity, ct);
            await BulkInsertAsync(connection, sqlTransaction, "RequestStatusHistory", history, SqlBulkCopyOptions.Default, ct);

            // Requests were inserted with explicit ids; new ones created through the API continue after them
            var reseed = string.Create(CultureInfo.InvariantCulture, $"DBCC CHECKIDENT ('Requests', RESEED, {requestCount})");
            await db.Database.ExecuteSqlRawAsync(reseed, ct);
            await transaction.CommitAsync(ct);
        }

        LogSeeded(requestCount, stopwatch.ElapsedMilliseconds);
        await RebuildIndexesIfFragmentedAsync(ct);
    }

    private (DataTable Requests, DataTable History) Generate(int count)
    {
        var random = new Random(RandomSeed);
        // Start of today (UTC): runs on the same day produce identical dates
        var anchor = timeProvider.GetUtcNow().UtcDateTime.Date;
        // A day inside each end of the two-year window: the anchor is earlier than "now", never later
        var oldest = anchor.AddYears(-2).AddDays(1);
        var windowInMinutes = (int)(anchor.AddDays(-1) - oldest).TotalMinutes;
        var requests = RequestsTable();
        var history = HistoryTable();

        for (var id = 1; id <= count; id++)
        {
            var status = Pick(random, StatusWeights);
            var priority = Pick(random, PriorityWeights);
            var handler = HebrewSampleData.Handlers[random.Next(HebrewSampleData.Handlers.Length)];
            // Half of the new requests are not assigned yet; every request in progress has a handler
            string? assignedTo = status == RequestStatus.New && random.Next(2) == 0 ? null : handler;
            var createdAt = oldest.AddMinutes(random.Next(windowInMinutes));

            var path = PathFromNew[status];
            var changedAt = createdAt;
            for (var step = 1; step < path.Length; step++)
            {
                // Each change happens later than the one before it, and never after the anchor
                var minutesLeft = (int)(anchor - changedAt).TotalMinutes;
                changedAt = changedAt.AddMinutes(1 + random.Next(minutesLeft / (path.Length - step + 1)));
                history.Rows.Add(id, (byte)path[step - 1], (byte)path[step], changedAt, handler);
            }

            requests.Rows.Add(id, HebrewSampleData.Titles[random.Next(HebrewSampleData.Titles.Length)],
                HebrewSampleData.Organizations[random.Next(HebrewSampleData.Organizations.Length)],
                (byte)status, (byte)priority, assignedTo, createdAt, changedAt);
        }

        return (requests, history);
    }

    private static T Pick<T>(Random random, (T Value, int Weight)[] weights)
    {
        var roll = random.Next(100);
        foreach (var (value, weight) in weights)
        {
            if ((roll -= weight) < 0)
            {
                return value;
            }
        }

        throw new InvalidOperationException("Weights must add up to 100.");
    }

    private static RequestStatus[] ShortestPathFromNew(RequestStatus target)
    {
        // Breadth-first search over the transition rules, so the generated history always follows them
        var cameFrom = new Dictionary<RequestStatus, RequestStatus>();
        var queue = new Queue<RequestStatus>([RequestStatus.New]);
        while (queue.TryDequeue(out var current) && current != target)
        {
            foreach (var next in StatusTransitions.AllowedFrom(current))
            {
                if (next != RequestStatus.New && cameFrom.TryAdd(next, current))
                {
                    queue.Enqueue(next);
                }
            }
        }

        var path = new List<RequestStatus> { target };
        while (path[^1] != RequestStatus.New)
        {
            path.Add(cameFrom[path[^1]]);
        }

        path.Reverse();
        return [.. path];
    }

    private static async Task BulkInsertAsync(SqlConnection connection, SqlTransaction transaction, string table,
        DataTable rows, SqlBulkCopyOptions options, CancellationToken ct)
    {
        using var bulkCopy = new SqlBulkCopy(connection, options, transaction) { DestinationTableName = table, BatchSize = BatchSize };
        foreach (DataColumn column in rows.Columns)
        {
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        await bulkCopy.WriteToServerAsync(rows, ct);
    }

    private async Task RebuildIndexesIfFragmentedAsync(CancellationToken ct)
    {
        var fragmentation = await db.Database.SqlQuery<double>($"""
            SELECT ISNULL(MAX(avg_fragmentation_in_percent), 0) AS Value
            FROM sys.dm_db_index_physical_stats(DB_ID(), OBJECT_ID('Requests'), NULL, NULL, 'LIMITED')
            """).SingleAsync(ct);
        LogFragmentation(fragmentation);

        if (fragmentation > 30)
        {
            await db.Database.ExecuteSqlRawAsync("ALTER INDEX ALL ON Requests REBUILD", ct);
            LogIndexesRebuilt();
        }
    }

    private static DataTable RequestsTable() => Table(
        ("Id", typeof(int)), ("Title", typeof(string)), ("OrganizationName", typeof(string)), ("Status", typeof(byte)),
        ("Priority", typeof(byte)), ("AssignedTo", typeof(string)), ("CreatedAt", typeof(DateTime)), ("UpdatedAt", typeof(DateTime)));

    private static DataTable HistoryTable() => Table(
        ("RequestId", typeof(int)), ("PreviousStatus", typeof(byte)), ("NewStatus", typeof(byte)),
        ("ChangedAt", typeof(DateTime)), ("ChangedBy", typeof(string)));

    private static DataTable Table(params (string Name, Type Type)[] columns)
    {
        var table = new DataTable { Locale = CultureInfo.InvariantCulture };
        foreach (var (name, type) in columns)
        {
            table.Columns.Add(name, type);
        }

        return table;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Seeded {Count} requests in {ElapsedMs} ms")]
    private partial void LogSeeded(int count, long elapsedMs);

    [LoggerMessage(Level = LogLevel.Information, Message = "Requests index fragmentation after seeding: {Fragmentation:F1}%")]
    private partial void LogFragmentation(double fragmentation);

    [LoggerMessage(Level = LogLevel.Information, Message = "Rebuilt the Requests indexes")]
    private partial void LogIndexesRebuilt();
}
