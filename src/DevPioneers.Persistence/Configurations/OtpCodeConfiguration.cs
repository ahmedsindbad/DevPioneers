// ============================================
// File: DevPioneers.Persistence/Configurations/OtpCodeConfiguration.cs
// ============================================
using DevPioneers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevPioneers.Persistence.Configurations;

public class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        // Table name
        builder.ToTable("OtpCodes");

        // Primary key
        builder.HasKey(o => o.Id);

        // Properties
        builder.Property(o => o.UserId)
            .IsRequired();

        builder.Property(o => o.Code)
            .IsRequired()
            .HasMaxLength(10); // 6 digits usually, but allow some flexibility

        builder.Property(o => o.Purpose)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(o => o.ExpiresAt)
            .IsRequired();

        builder.Property(o => o.IsUsed)
            .HasDefaultValue(false);

        builder.Property(o => o.UsedAt)
            .IsRequired(false);

        builder.Property(o => o.UsedFromIp)
            .HasMaxLength(45) // IPv6 support
            .IsRequired(false);

        builder.Property(o => o.AttemptCount)
            .HasDefaultValue(0);

        // Indexes for performance
        builder.HasIndex(o => o.UserId)
            .HasDatabaseName("IX_OtpCodes_UserId");

        builder.HasIndex(o => new { o.Code, o.UserId })
            .HasDatabaseName("IX_OtpCodes_Code_UserId");

        builder.HasIndex(o => o.ExpiresAt)
            .HasDatabaseName("IX_OtpCodes_ExpiresAt");

        builder.HasIndex(o => new { o.UserId, o.IsUsed, o.ExpiresAt })
            .HasDatabaseName("IX_OtpCodes_UserId_IsUsed_ExpiresAt");

        // Relationships
        builder.HasOne(o => o.User)
            .WithMany(u => u.OtpCodes)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}