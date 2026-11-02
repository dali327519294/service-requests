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
        builder.HasIndex(r => r.CreatedAt).IsDescending();
        builder.HasIndex(r => new { r.Status, r.CreatedAt }).IsDescending(false, true).IncludeProperties(r => r.Priority);
        builder.HasIndex(r => new { r.AssignedTo, r.Status });
        builder.HasIndex(r => r.OrganizationName);
    }
}
