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
        builder.ToTable("OtpCodes");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .IsRequired()
            .ValueGeneratedOnAdd();

        builder.Property(o => o.Mobile)
            .HasMaxLength(15)
            .IsRequired();

        builder.Property(o => o.Email)
            .HasMaxLength(320)
            .IsRequired(false);

        builder.Property(o => o.Code)
            .HasMaxLength(100) // Hashed code can be longer
            .IsRequired();

        builder.Property(o => o.Purpose)
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue("Login");

        builder.Property(o => o.ExpiresAt)
            .IsRequired();

        builder.Property(o => o.VerifiedAt)
            .IsRequired(false);

        builder.Property(o => o.Attempts)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(o => o.MaxAttempts)
            .IsRequired()
            .HasDefaultValue(3);

        builder.Property(o => o.IpAddress)
            .HasMaxLength(45)
            .IsRequired(false);

        builder.Property(o => o.CreatedAtUtc)
            .IsRequired();

        builder.Property(o => o.UpdatedAtUtc)
            .IsRequired(false);

        // Foreign key to User (nullable for registration flow)
        builder.HasOne(o => o.User)
            .WithMany(u => u.OtpCodes)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired(false);

        // Indexes for performance
        builder.HasIndex(o => o.Mobile)
            .HasDatabaseName("IX_OtpCodes_Mobile");

        builder.HasIndex(o => o.Email)
            .HasDatabaseName("IX_OtpCodes_Email");

        builder.HasIndex(o => o.Code)
            .HasDatabaseName("IX_OtpCodes_Code");

        builder.HasIndex(o => o.ExpiresAt)
            .HasDatabaseName("IX_OtpCodes_ExpiresAt");

        builder.HasIndex(o => new { o.Mobile, o.Purpose, o.ExpiresAt })
            .HasDatabaseName("IX_OtpCodes_Mobile_Purpose_ExpiresAt");

        builder.HasIndex(o => new { o.Email, o.Purpose, o.ExpiresAt })
            .HasDatabaseName("IX_OtpCodes_Email_Purpose_ExpiresAt");

        // Add computed columns or constraints if needed
        builder.HasCheckConstraint("CK_OtpCodes_EmailOrMobile", 
            "([Email] IS NOT NULL) OR ([Mobile] IS NOT NULL)");
    }
}