using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ServiceRequests.Infrastructure.Persistence;
using ServiceRequests.Infrastructure.Persistence.Seeding;

namespace ServiceRequests.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Interceptors registered elsewhere (e.g. by tests) are picked up without changing this method
        services.AddDbContext<ServiceRequestsDbContext>((sp, options) => options
            .UseSqlServer(configuration.GetConnectionString("ServiceRequestsDb")
                ?? throw new InvalidOperationException("Connection string 'ServiceRequestsDb' is missing."))
            .AddInterceptors(sp.GetServices<ISaveChangesInterceptor>()));
        services.AddScoped<DataSeeder>();
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
