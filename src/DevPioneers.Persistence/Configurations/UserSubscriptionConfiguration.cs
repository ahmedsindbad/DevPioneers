// ============================================
// File: DevPioneers.Persistence/Configurations/UserSubscriptionConfiguration.cs
// ============================================
using DevPioneers.Domain.Entities;
using DevPioneers.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevPioneers.Persistence.Configurations;

public class UserSubscriptionConfiguration : IEntityTypeConfiguration<UserSubscription>
{
    public void Configure(EntityTypeBuilder<UserSubscription> builder)
    {
        // Table name
        builder.ToTable("UserSubscriptions");

        // Primary key
        builder.HasKey(us => us.Id);

        // Properties
        builder.Property(us => us.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(us => us.CancellationReason)
            .HasMaxLength(500);

        builder.Property(us => us.AutoRenewal)
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(us => new { us.UserId, us.Status })
            .HasDatabaseName("IX_UserSubscriptions_UserId_Status");

        builder.HasIndex(us => us.EndDate)
            .HasDatabaseName("IX_UserSubscriptions_EndDate");

        builder.HasIndex(us => us.NextBillingDate)
            .HasDatabaseName("IX_UserSubscriptions_NextBillingDate")
            .HasFilter("[NextBillingDate] IS NOT NULL");

        // Relationships configured in SubscriptionPlan and User
        builder.HasOne(us => us.Payment)
            .WithMany(p => p.Subscriptions)
            .HasForeignKey(us => us.PaymentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}