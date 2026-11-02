using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceRequests.Domain;
using ServiceRequests.Domain.Users;

namespace ServiceRequests.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users", t => t.HasCheckConstraint("CK_Users_Role", CheckConstraints.InEnum<UserRole>("Role")));

        builder.Property(u => u.UserName).HasMaxLength(FieldLengths.UserName);
        builder.Property(u => u.DisplayName).HasMaxLength(FieldLengths.DisplayName);
        builder.Property(u => u.PasswordHash).HasMaxLength(FieldLengths.PasswordHash);
        builder.Property(u => u.Role).HasConversion<byte>();
        builder.HasIndex(u => u.UserName).IsUnique();
    }
}
