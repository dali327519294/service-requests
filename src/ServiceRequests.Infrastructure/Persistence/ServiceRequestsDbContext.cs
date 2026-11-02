using Microsoft.EntityFrameworkCore;
using ServiceRequests.Domain.Requests;
using ServiceRequests.Domain.Users;

namespace ServiceRequests.Infrastructure.Persistence;

public class ServiceRequestsDbContext(DbContextOptions<ServiceRequestsDbContext> options) : DbContext(options)
{
    public DbSet<ServiceRequest> Requests => Set<ServiceRequest>();
    public DbSet<RequestStatusHistory> StatusHistory => Set<RequestStatusHistory>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ServiceRequestsDbContext).Assembly);

    // SQL Server keeps no time zone: every date is saved as UTC and read back marked as UTC
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTime>().HavePrecision(3).HaveConversion<UtcDateTimeConverter>();
}
