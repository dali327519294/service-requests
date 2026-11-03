using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ServiceRequests.Domain.Requests;

namespace ServiceRequests.IntegrationTests.Infrastructure;

/// <summary>Test-only: on the next save, changes a request on a separate connection first - "another user was faster".</summary>
public sealed class RaceInjector(string connectionString) : SaveChangesInterceptor
{
    private sealed record Race(int RequestId, RequestStatus Status);

    private Race? _armed;

    public void Arm(int requestId, RequestStatus sneakStatus) => Volatile.Write(ref _armed, new Race(requestId, sneakStatus));

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        // Fires once: the next save, whoever makes it
        if (Interlocked.Exchange(ref _armed, null) is { } race)
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand(
                "UPDATE Requests SET Status = @status, UpdatedAt = SYSUTCDATETIME() WHERE Id = @id", connection);
            command.Parameters.AddWithValue("@status", (byte)race.Status);
            command.Parameters.AddWithValue("@id", race.RequestId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
