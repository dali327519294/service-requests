using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceRequests.Domain;
using ServiceRequests.Domain.Requests;

namespace ServiceRequests.Infrastructure.Persistence.Configurations;

internal sealed class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.ToTable("Requests", t =>
        {
            t.HasCheckConstraint("CK_Requests_Status", CheckConstraints.InEnum<RequestStatus>("Status"));
            t.HasCheckConstraint("CK_Requests_Priority", CheckConstraints.InEnum<RequestPriority>("Priority"));
        });

        builder.Property(r => r.Title).HasMaxLength(FieldLengths.Title);
        builder.Property(r => r.OrganizationName).HasMaxLength(FieldLengths.OrganizationName);
        builder.Property(r => r.AssignedTo).HasMaxLength(FieldLengths.AssignedTo);
        builder.Property(r => r.Status).HasConversion<byte>();
        builder.Property(r => r.Priority).HasConversion<byte>();
        builder.Property(r => r.RowVersion).IsRowVersion();

        // Spec §4: each index serves a specific screen query
        // Id is part of the key, in the same direction as the date: the list sorts by (CreatedAt, Id), Id breaking ties,
        // and an index hands rows back already sorted only when its order matches exactly. Without Id here,
        // SQL Server appends it ascending and every list page sorts the whole table (docs/PERFORMANCE.md)
        builder.HasIndex(r => new { r.CreatedAt, r.Id }).IsDescending(true, true)
            .HasDatabaseName("IX_Requests_CreatedAt");
        builder.HasIndex(r => new { r.Status, r.CreatedAt, r.Id }).IsDescending(false, true, true)
            .IncludeProperties(r => r.Priority)
            .HasDatabaseName("IX_Requests_Status_CreatedAt");
        builder.HasIndex(r => new { r.AssignedTo, r.Status });
        builder.HasIndex(r => r.OrganizationName);
    }
}
