using Microsoft.EntityFrameworkCore;
using ServiceRequests.Application.Common;
using ServiceRequests.Application.Requests.Contracts;
using ServiceRequests.Application.Requests.Summary;
using ServiceRequests.Domain.Requests;
using ServiceRequests.Infrastructure.Persistence;

namespace ServiceRequests.Application.Requests.Commands;

public sealed class RequestStatusService(ServiceRequestsDbContext db, ICurrentUser currentUser, TimeProvider timeProvider,
    SummaryCache summaryCache)
{
    private const string ConflictMessage = "The request was changed by someone else.";

    /// <remarks>Expects a validated request: Status is defined and RowVersion decodes to 8 bytes.</remarks>
    public async Task<UpdateStatusOutcome> UpdateStatusAsync(int id, UpdateStatusRequest update, CancellationToken ct)
    {
        var newStatus = update.Status ?? throw new ArgumentException("Status is required.", nameof(update));
        if (!RowVersionCodec.TryDecode(update.RowVersion, out var clientVersion))
        {
            throw new ArgumentException("RowVersion must be validated before it gets here.", nameof(update));
        }

        var changedBy = currentUser.UserName;   // no user, no database work

        var request = await db.Requests.SingleOrDefaultAsync(r => r.Id == id, ct);
        if (request is null)
        {
            return new UpdateStatusOutcome.NotFound();
        }

        // Version before transition: a transition judged against an old state gives a misleading error
        if (!request.RowVersion.AsSpan().SequenceEqual(clientVersion))
        {
            return new UpdateStatusOutcome.Conflict(request.ToDetails());
        }

        var previous = request.Status;
        if (!StatusTransitions.IsAllowed(previous, newStatus))
        {
            return new UpdateStatusOutcome.InvalidTransition(previous, newStatus, StatusTransitions.AllowedFrom(previous));
        }

        db.StatusHistory.Add(request.ChangeStatus(newStatus, changedBy, Now()));
        try
        {
            // One transaction: UPDATE ... WHERE RowVersion = <the version read above>, and the history INSERT
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Someone saved between our read and our write: their change stays, ours and its history row roll back
            db.ChangeTracker.Clear();
            var current = await db.Requests.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id, ct);
            return current is null ? new UpdateStatusOutcome.NotFound() : new UpdateStatusOutcome.Conflict(current.ToDetails());
        }

        summaryCache.Invalidate();   // after the commit, never before
        return new UpdateStatusOutcome.Updated(request.ToDetails(), previous);
    }

    /// <remarks>Expects a validated request. Each item gets its own outcome; the valid ones are saved together.</remarks>
    public async Task<BulkUpdateResult> BulkUpdateAsync(BulkUpdateStatusRequest bulk, CancellationToken ct)
    {
        var newStatus = bulk.Status ?? throw new ArgumentException("Status is required.", nameof(bulk));
        var items = bulk.Items?.Select(i => i ?? throw new ArgumentException("Items must be validated first.", nameof(bulk))).ToList()
            ?? throw new ArgumentException("Items are required.", nameof(bulk));
        var changedBy = currentUser.UserName;
        var now = Now();

        // One query for the whole batch
        var ids = items.Select(i => i.Id).ToList();
        var requests = await db.Requests.Where(r => ids.Contains(r.Id)).ToDictionaryAsync(r => r.Id, ct);

        var results = new BulkItemResult[items.Count];
        var pending = new Dictionary<ServiceRequest, (int Index, RequestStatusHistory History)>();
        for (var i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (!requests.TryGetValue(item.Id, out var request))
            {
                results[i] = Failed(item.Id, BulkOutcome.NotFound, "The request does not exist.");
            }
            else if (!request.RowVersion.AsSpan().SequenceEqual(Decode(item.RowVersion)))
            {
                results[i] = Failed(item.Id, BulkOutcome.Conflict, ConflictMessage);
            }
            else if (!StatusTransitions.IsAllowed(request.Status, newStatus))
            {
                results[i] = Failed(item.Id, BulkOutcome.InvalidTransition, $"A request in status {request.Status} cannot move to {newStatus}.");
            }
            else
            {
                var history = request.ChangeStatus(newStatus, changedBy, now);
                db.StatusHistory.Add(history);
                pending.Add(request, (i, history));
            }
        }

        while (pending.Count > 0)
        {
            try
            {
                await db.SaveChangesAsync(ct);
                foreach (var (request, (index, _)) in pending)
                {
                    results[index] = new BulkItemResult(request.Id, BulkOutcome.Updated, null, RowVersionCodec.Encode(request.RowVersion));
                }

                break;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // The whole transaction rolled back. Take out the rows that changed under us and save the rest again;
                // every round removes at least one, so the loop always ends
                var conflicted = ex.Entries.Select(e => e.Entity).OfType<ServiceRequest>().Where(pending.ContainsKey).ToList();
                if (conflicted.Count == 0)
                {
                    throw;
                }

                foreach (var request in conflicted)
                {
                    var (index, history) = pending[request];
                    db.Entry(history).State = EntityState.Detached;
                    db.Entry(request).State = EntityState.Detached;
                    pending.Remove(request);
                    results[index] = Failed(request.Id, BulkOutcome.Conflict, ConflictMessage);
                }
            }
        }

        var succeeded = results.Count(r => r.Outcome == BulkOutcome.Updated);
        if (succeeded > 0)
        {
            summaryCache.Invalidate();   // once for the batch, after the commit
        }

        return new BulkUpdateResult(items.Count, succeeded, items.Count - succeeded, results);
    }

    private static byte[] Decode(string? rowVersion) => RowVersionCodec.TryDecode(rowVersion, out var version)
        ? version
        : throw new ArgumentException("RowVersion must be validated before it gets here.", nameof(rowVersion));

    private static BulkItemResult Failed(int id, BulkOutcome outcome, string error) => new(id, outcome, error, null);

    // Dates are stored to the millisecond (HavePrecision(3) in ServiceRequestsDbContext), and SQL Server cuts the
    // rest. Taking "now" at that precision keeps the answer of an update equal to what any later read returns
    private DateTime Now()
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerMillisecond));
    }
}
