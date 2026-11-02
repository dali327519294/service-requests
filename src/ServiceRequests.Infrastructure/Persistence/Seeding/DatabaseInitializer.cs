using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ServiceRequests.Infrastructure.Persistence.Seeding;

public static class DatabaseInitializer
{
    /// <summary>Applies migrations and, on an empty database, seeds the default demo data.</summary>
    public static async Task InitializeAsync(IServiceProvider services, CancellationToken ct)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ServiceRequestsDbContext>();
        await db.Database.MigrateAsync(ct);
        if (!await db.Requests.AnyAsync(ct))
        {
            await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync(DataSeeder.DefaultCount, ct);
        }
    }

    /// <summary>Applies migrations and replaces all data with <paramref name="requestCount"/> generated requests.</summary>
    public static async Task ReseedAsync(IServiceProvider services, int requestCount, CancellationToken ct)
    {
        using var scope = services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ServiceRequestsDbContext>().Database.MigrateAsync(ct);
        await scope.ServiceProvider.GetRequiredService<DataSeeder>().SeedAsync(requestCount, ct);
    }
}
