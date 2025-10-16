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
        builder.HasKey(at => at.Id);

        // Properties
        builder.Property(at => at.UserFullName)
            .HasMaxLength(200);

        builder.Property(at => at.EntityName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(at => at.Action)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(at => at.IpAddress)
            .HasMaxLength(45);

        builder.Property(at => at.UserAgent)
            .HasMaxLength(500);

        builder.Property(at => at.RequestPath)
            .HasMaxLength(500);

        builder.Property(at => at.RequestMethod)
            .HasMaxLength(10);

        builder.Property(at => at.TimestampUtc)
            .HasDefaultValueSql("GETUTCDATE()");

        // Indexes
        builder.HasIndex(at => at.UserId)
            .HasDatabaseName("IX_AuditTrails_UserId")
            .HasFilter("[UserId] IS NOT NULL");

        builder.HasIndex(at => new { at.EntityName, at.EntityId })
            .HasDatabaseName("IX_AuditTrails_Entity")
            .HasFilter("[EntityId] IS NOT NULL");

        builder.HasIndex(at => at.Action)
            .HasDatabaseName("IX_AuditTrails_Action");

        builder.HasIndex(at => at.TimestampUtc)
            .HasDatabaseName("IX_AuditTrails_Timestamp");

        // For cleanup job - composite index
        builder.HasIndex(at => new { at.TimestampUtc, at.EntityName })
            .HasDatabaseName("IX_AuditTrails_Timestamp_Entity");
    }
}