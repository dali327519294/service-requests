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

        db.StatusHistory.Add(request.ChangeStatus(newStatus, changedBy, timeProvider.GetUtcNow().UtcDateTime));
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
}
