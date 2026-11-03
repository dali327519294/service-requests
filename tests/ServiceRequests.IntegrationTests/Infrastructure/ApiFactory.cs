using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using ServiceRequests.Application.Common;
using ServiceRequests.Infrastructure.Persistence;

namespace ServiceRequests.IntegrationTests.Infrastructure;

/// <summary>Runs the real API in memory against its own migrated database, deleted when the tests finish.</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _connectionString = TestDatabase.CreateConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("ConnectionStrings:ServiceRequestsDb", _connectionString);
        // Tests seed exactly the data they need; 100,000 demo rows per test class would only slow them down
        builder.UseSetting("Database:InitializeOnStartup", "false");
        builder.ConfigureServices(services =>
        {
            services.AddTransient<IStartupFilter, TestEndpoints>();
            // Lets a test force "another user saved first" between a request's read and its write
            services.AddSingleton(new RaceInjector(_connectionString));
            services.AddSingleton<ISaveChangesInterceptor>(sp => sp.GetRequiredService<RaceInjector>());
        });
    }

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

    // Endpoints that exist only in tests, to exercise the error pipeline and the current user
    private sealed class TestEndpoints : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);
            app.Map("/test/throw", branch => branch.Run(_ => throw new InvalidOperationException("Secret internal detail")));
            app.Map("/test/user", branch => branch.Run(context =>
                context.Response.WriteAsync(context.RequestServices.GetRequiredService<ICurrentUser>().UserName)));
        };
    }
}
