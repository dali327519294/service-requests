using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceRequests.Infrastructure.Persistence;

namespace ServiceRequests.IntegrationTests.Infrastructure;

/// <summary>Runs the real API in memory against its own migrated database, deleted when the tests finish.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _connectionString = TestDatabase.CreateConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:ServiceRequests", _connectionString);

    public Task InitializeAsync() => ExecuteDbAsync(db => db.Database.MigrateAsync());

    async Task IAsyncLifetime.DisposeAsync()
    {
        await ExecuteDbAsync(db => db.Database.EnsureDeletedAsync());
        await DisposeAsync();
    }

    public async Task ExecuteDbAsync(Func<ServiceRequestsDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        await action(scope.ServiceProvider.GetRequiredService<ServiceRequestsDbContext>());
    }
}
