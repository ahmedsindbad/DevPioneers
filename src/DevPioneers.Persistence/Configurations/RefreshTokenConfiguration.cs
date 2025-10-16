// ============================================
// File: DevPioneers.Persistence/Configurations/RefreshTokenConfiguration.cs
// ============================================
using DevPioneers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevPioneers.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        // Table name
        builder.ToTable("RefreshTokens");

        // Primary key
        builder.HasKey(rt => rt.Id);

        // Properties
        builder.Property(rt => rt.Token)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(rt => rt.CreatedByIp)
            .HasMaxLength(45);

        builder.Property(rt => rt.UsedByIp)
            .HasMaxLength(45);

        builder.Property(rt => rt.RevokedByIp)
            .HasMaxLength(45);

        builder.Property(rt => rt.UserAgent)
            .HasMaxLength(500);

        builder.Property(rt => rt.DeviceId)
            .HasMaxLength(200);

        builder.Property(rt => rt.RevokedReason)
            .HasMaxLength(500);

        builder.Property(rt => rt.ReplacedByToken)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(rt => rt.Token)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_Token");

        builder.HasIndex(rt => new { rt.UserId, rt.ExpiresAt })
            .HasDatabaseName("IX_RefreshTokens_UserId_ExpiresAt");

        builder.HasIndex(rt => rt.ExpiresAt)
            .HasDatabaseName("IX_RefreshTokens_ExpiresAt");

        // For cleanup job
        builder.HasIndex(rt => new { rt.RevokedAt, rt.ExpiresAt })
            .HasDatabaseName("IX_RefreshTokens_Revoked_Expired");
    }
}
