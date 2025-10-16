// ============================================
// File: DevPioneers.Persistence/Configurations/UserRoleConfiguration.cs
// ============================================
using DevPioneers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevPioneers.Persistence.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        // Table name
        builder.ToTable("UserRoles");

        // Primary key
        builder.HasKey(ur => ur.Id);

        // Alternate key (composite unique)
        builder.HasIndex(ur => new { ur.UserId, ur.RoleId })
            .IsUnique()
            .HasDatabaseName("IX_UserRoles_UserId_RoleId");

        // Properties
        builder.Property(ur => ur.AssignedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Relationships configured in User and Role entities
    }
}
