// ============================================
// File: DevPioneers.Persistence/Configurations/RoleConfiguration.cs
// ============================================
using DevPioneers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevPioneers.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        // Table name
        builder.ToTable("Roles");

        // Primary key
        builder.HasKey(r => r.Id);

        // Properties
        builder.Property(r => r.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.NormalizedName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(r => r.Description)
            .HasMaxLength(500);

        builder.Property(r => r.IsSystemRole)
            .HasDefaultValue(false);

        // Indexes
        builder.HasIndex(r => r.Name)
            .IsUnique()
            .HasDatabaseName("IX_Roles_Name");

        builder.HasIndex(r => r.NormalizedName)
            .IsUnique()
            .HasDatabaseName("IX_Roles_NormalizedName");

        // Relationships
        builder.HasMany(r => r.UserRoles)
            .WithOne(ur => ur.Role)
            .HasForeignKey(ur => ur.RoleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed data
        builder.HasData(
            new Role
            {
                Id = 1,
                Name = "Admin",
                NormalizedName = "ADMIN",
                Description = "System Administrator with full access",
                IsSystemRole = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new Role
            {
                Id = 2,
                Name = "Manager",
                NormalizedName = "MANAGER",
                Description = "Manager with elevated permissions",
                IsSystemRole = true,
                CreatedAtUtc = DateTime.UtcNow
            },
            new Role
            {
                Id = 3,
                Name = "User",
                NormalizedName = "USER",
                Description = "Regular user with standard access",
                IsSystemRole = true,
                CreatedAtUtc = DateTime.UtcNow
            }
        );
    }
}