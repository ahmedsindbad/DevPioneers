// ============================================
// File: DevPioneers.Persistence/Configurations/UserConfiguration.cs
// ============================================
using DevPioneers.Domain.Entities;
using DevPioneers.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevPioneers.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table name
        builder.ToTable("Users");

        // Primary key
        builder.HasKey(u => u.Id);

        // Properties
        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(u => u.Mobile)
            .HasMaxLength(20);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(u => u.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(u => u.ProfilePictureUrl)
            .HasMaxLength(500);

        builder.Property(u => u.TwoFactorSecretKey)
            .HasMaxLength(100);

        builder.Property(u => u.LastLoginIp)
            .HasMaxLength(45); // IPv6 support

        builder.Property(u => u.PasswordResetToken)
            .HasMaxLength(500);

        builder.Property(u => u.EmailVerificationToken)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        builder.HasIndex(u => u.Mobile)
            .IsUnique()
            .HasDatabaseName("IX_Users_Mobile")
            .HasFilter("[Mobile] IS NOT NULL");

        builder.HasIndex(u => u.Status)
            .HasDatabaseName("IX_Users_Status");

        builder.HasIndex(u => new { u.Email, u.Status })
            .HasDatabaseName("IX_Users_Email_Status");

        // Relationships
        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Subscriptions)
            .WithOne(s => s.User)
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(u => u.Wallet)
            .WithOne(w => w.User)
            .HasForeignKey<Wallet>(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(u => u.Payments)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Default values
        builder.Property(u => u.Status)
            .HasDefaultValue(UserStatus.Pending);

        builder.Property(u => u.EmailVerified)
            .HasDefaultValue(false);

        builder.Property(u => u.MobileVerified)
            .HasDefaultValue(false);

        builder.Property(u => u.TwoFactorEnabled)
            .HasDefaultValue(false);

        builder.Property(u => u.FailedLoginAttempts)
            .HasDefaultValue(0);

        builder.Property(u => u.CreatedAtUtc)
            .HasDefaultValueSql("GETUTCDATE()");
    }
}