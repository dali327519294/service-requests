using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceRequests.Domain;
using ServiceRequests.Domain.Requests;

namespace ServiceRequests.Infrastructure.Persistence.Configurations;

internal sealed class RequestStatusHistoryConfiguration : IEntityTypeConfiguration<RequestStatusHistory>
{
    public void Configure(EntityTypeBuilder<RequestStatusHistory> builder)
    {
        builder.ToTable("RequestStatusHistory", t =>
        {
            t.HasCheckConstraint("CK_RequestStatusHistory_PreviousStatus", CheckConstraints.InEnum<RequestStatus>("PreviousStatus"));
            t.HasCheckConstraint("CK_RequestStatusHistory_NewStatus", CheckConstraints.InEnum<RequestStatus>("NewStatus"));
        });

        builder.Property(h => h.PreviousStatus).HasConversion<byte>();
        builder.Property(h => h.NewStatus).HasConversion<byte>();
        builder.Property(h => h.ChangedBy).HasMaxLength(FieldLengths.ChangedBy);

        // Restrict: deleting a request must never silently delete its audit trail
        builder.HasOne<ServiceRequest>().WithMany().HasForeignKey(h => h.RequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(h => new { h.RequestId, h.ChangedAt }).IsDescending(false, true);
    }
}
