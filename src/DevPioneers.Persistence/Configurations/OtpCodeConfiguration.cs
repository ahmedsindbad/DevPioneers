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
        builder.HasKey(otp => otp.Id);

        // Properties
        builder.Property(otp => otp.Mobile)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(otp => otp.Email)
            .HasMaxLength(256);

        builder.Property(otp => otp.Code)
            .IsRequired()
            .HasMaxLength(100); // Hashed

        builder.Property(otp => otp.Purpose)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(otp => otp.IpAddress)
            .HasMaxLength(45);

        builder.Property(otp => otp.Attempts)
            .HasDefaultValue(0);

        builder.Property(otp => otp.MaxAttempts)
            .HasDefaultValue(3);

        // Indexes
        builder.HasIndex(otp => new { otp.Mobile, otp.Purpose, otp.ExpiresAt })
            .HasDatabaseName("IX_OtpCodes_Mobile_Purpose_Expires");

        builder.HasIndex(otp => otp.ExpiresAt)
            .HasDatabaseName("IX_OtpCodes_ExpiresAt");

        builder.HasIndex(otp => otp.UserId)
            .HasDatabaseName("IX_OtpCodes_UserId")
            .HasFilter("[UserId] IS NOT NULL");
    }
}
