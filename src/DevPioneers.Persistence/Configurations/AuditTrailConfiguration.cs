// ============================================
// File: DevPioneers.Persistence/Configurations/AuditTrailConfiguration.cs
// ============================================
using DevPioneers.Domain.Entities;
using DevPioneers.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevPioneers.Persistence.Configurations;

public class AuditTrailConfiguration : IEntityTypeConfiguration<AuditTrail>
{
    public void Configure(EntityTypeBuilder<AuditTrail> builder)
    {
        // Table name
        builder.ToTable("AuditTrails");

        // Primary key
        builder.HasKey(a => a.Id);

        // Properties
        builder.Property(a => a.UserId)
            .IsRequired(false); // Nullable for system actions

        builder.Property(a => a.UserFullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.EntityName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.EntityId)
            .IsRequired(false); // Nullable for bulk operations

        builder.Property(a => a.Action)
            .IsRequired()
            .HasConversion<int>(); // Store enum as int

        builder.Property(a => a.OldValues)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(a => a.NewValues)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(a => a.Timestamp)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(a => a.IpAddress)
            .HasMaxLength(45) // IPv6 support
            .IsRequired(false);

        builder.Property(a => a.UserAgent)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(a => a.RequestMethod)
            .HasMaxLength(10)
            .IsRequired(false);

        builder.Property(a => a.RequestUrl)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(a => a.DurationMs)
            .IsRequired(false);

        builder.Property(a => a.ExceptionDetails)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        builder.Property(a => a.Metadata)
            .HasColumnType("nvarchar(max)")
            .IsRequired(false);

        // Indexes for performance
        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_AuditTrails_UserId");

        builder.HasIndex(a => a.EntityName)
            .HasDatabaseName("IX_AuditTrails_EntityName");

        builder.HasIndex(a => a.Action)
            .HasDatabaseName("IX_AuditTrails_Action");

        builder.HasIndex(a => a.Timestamp)
            .HasDatabaseName("IX_AuditTrails_Timestamp");

        builder.HasIndex(a => new { a.EntityName, a.EntityId })
            .HasDatabaseName("IX_AuditTrails_EntityName_EntityId");

        builder.HasIndex(a => new { a.UserId, a.Timestamp })
            .HasDatabaseName("IX_AuditTrails_UserId_Timestamp");

        // Relationships
        builder.HasOne(a => a.User)
            .WithMany() // User doesn't need navigation back to audit trails
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull); // Keep audit even if user is deleted
    }
}