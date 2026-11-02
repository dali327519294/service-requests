using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using ServiceRequests.Domain;
using ServiceRequests.Domain.Requests;
using ServiceRequests.Infrastructure.Persistence.Seeding;
using ServiceRequests.IntegrationTests.Infrastructure;

namespace ServiceRequests.IntegrationTests.Persistence;

public class SeederTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    // A fixed "now": the same seed gives the same dates on any day
    private static readonly TimeProvider FixedTime = new FixedTimeProvider(new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero));

    [Fact]
    public async Task Seed_is_deterministic()
    {
        var first = await SeedAndReadAsync(500);
        var second = await SeedAndReadAsync(500);

        Assert.Equal(first, second);
    }

    [Fact]
    public async Task Reseed_replaces_the_data_and_restarts_ids()
    {
        await SeedAsync(500);
        await SeedAsync(300);

        await factory.ExecuteDbAsync(async db =>
        {
            Assert.Equal(300, await db.Requests.CountAsync());
            Assert.Equal(300, await db.Requests.MaxAsync(r => r.Id));
        });
    }

    [Fact]
    public async Task Seed_history_leads_from_new_to_current_status()
    {
        await SeedAsync(500);

        await factory.ExecuteDbAsync(async db =>
        {
            var history = (await db.StatusHistory.AsNoTracking().OrderBy(h => h.Id).ToListAsync()).ToLookup(h => h.RequestId);
            foreach (var request in await db.Requests.AsNoTracking().ToListAsync())
            {
                var status = RequestStatus.New;
                var time = request.CreatedAt;
                foreach (var step in history[request.Id])
                {
                    Assert.Equal(status, step.PreviousStatus);
                    Assert.True(StatusTransitions.IsAllowed(step.PreviousStatus, step.NewStatus));
                    Assert.True(step.ChangedAt > time);
                    (status, time) = (step.NewStatus, step.ChangedAt);
                }

                Assert.Equal(request.Status, status);
                Assert.Equal(time, request.UpdatedAt);
            }
        });
    }

    [Fact]
    public async Task Seed_status_distribution_matches_the_spec()
    {
        await SeedAsync(2000);

        Dictionary<RequestStatus, int> counts = [];
        await factory.ExecuteDbAsync(async db => counts = await db.Requests
            .GroupBy(r => r.Status).ToDictionaryAsync(g => g.Key, g => g.Count()));

        // Spec §5: 10% New, 40% In progress, 15% Waiting, 35% Completed (±3%)
        Assert.InRange(counts[RequestStatus.New] / 2000.0, 0.07, 0.13);
        Assert.InRange(counts[RequestStatus.InProgress] / 2000.0, 0.37, 0.43);
        Assert.InRange(counts[RequestStatus.Waiting] / 2000.0, 0.12, 0.18);
        Assert.InRange(counts[RequestStatus.Completed] / 2000.0, 0.32, 0.38);
    }

    [Fact]
    public async Task Seed_respects_field_limits_and_hebrew_text()
    {
        await SeedAsync(500);

        await factory.ExecuteDbAsync(async db =>
        {
            var requests = await db.Requests.AsNoTracking().ToListAsync();
            var now = FixedTime.GetUtcNow().UtcDateTime;

            Assert.All(requests, r => Assert.InRange(r.Title.Length, 1, FieldLengths.Title));
            Assert.All(requests, r => Assert.InRange(r.CreatedAt, now.AddYears(-2), now));
            Assert.Contains(requests, r => r.Title.Any(c => c is >= 'א' and <= 'ת'));
        });
    }

    [Fact]
    public async Task Seed_keeps_constraints_trusted()
    {
        await SeedAsync(500);

        List<string> untrusted = [];
        await factory.ExecuteDbAsync(async db => untrusted = await db.Database.SqlQuery<string>($"""
            SELECT name AS Value FROM sys.check_constraints WHERE is_not_trusted = 1
            UNION ALL
            SELECT name FROM sys.foreign_keys WHERE is_not_trusted = 1
            """).ToListAsync());

        Assert.Empty(untrusted);
    }

    private Task SeedAsync(int count) => factory.ExecuteDbAsync(db =>
        new DataSeeder(db, FixedTime, NullLogger<DataSeeder>.Instance).SeedAsync(count, CancellationToken.None));

    private async Task<List<(string, string, RequestStatus, RequestPriority, string?, DateTime)>> SeedAndReadAsync(int count)
    {
        await SeedAsync(count);
        List<(string, string, RequestStatus, RequestPriority, string?, DateTime)> rows = [];
        await factory.ExecuteDbAsync(async db => rows = (await db.Requests.AsNoTracking().OrderBy(r => r.Id).ToListAsync())
            .Select(r => (r.Title, r.OrganizationName, r.Status, r.Priority, r.AssignedTo, r.CreatedAt)).ToList());
        return rows;
    }
}
