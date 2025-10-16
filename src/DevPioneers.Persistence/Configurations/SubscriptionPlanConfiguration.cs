// ============================================
// File: DevPioneers.Persistence/Configurations/SubscriptionPlanConfiguration.cs
// ============================================
using DevPioneers.Domain.Entities;
using DevPioneers.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevPioneers.Persistence.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        // Table name
        builder.ToTable("SubscriptionPlans");

        // Primary key
        builder.HasKey(sp => sp.Id);

        // Properties
        builder.Property(sp => sp.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(sp => sp.Description)
            .HasMaxLength(1000);

        builder.Property(sp => sp.Price)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(sp => sp.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("EGP");

        builder.Property(sp => sp.BillingCycle)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(sp => sp.Features)
            .IsRequired()
            .HasDefaultValue("[]");

        builder.Property(sp => sp.IsActive)
            .HasDefaultValue(true);

        builder.Property(sp => sp.DisplayOrder)
            .HasDefaultValue(0);

        builder.Property(sp => sp.DiscountPercentage)
            .HasPrecision(5, 2)
            .HasDefaultValue(0);

        // Indexes
        builder.HasIndex(sp => sp.Name)
            .HasDatabaseName("IX_SubscriptionPlans_Name");

        builder.HasIndex(sp => new { sp.IsActive, sp.DisplayOrder })
            .HasDatabaseName("IX_SubscriptionPlans_Active_Order");

        // Relationships
        builder.HasMany(sp => sp.UserSubscriptions)
            .WithOne(us => us.SubscriptionPlan)
            .HasForeignKey(us => us.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed data - Sample Plans
        builder.HasData(
            new SubscriptionPlan
            {
                Id = 1,
                Name = "Basic",
                Description = "Perfect for individuals and small projects",
                Price = 99.00m,
                Currency = "EGP",
                BillingCycle = BillingCycle.Monthly,
                TrialDurationDays = 7,
                Features = "[\"1 User\",\"10 GB Storage\",\"Email Support\"]",
                MaxUsers = 1,
                MaxStorageGb = 10,
                PointsAwarded = 100,
                IsActive = true,
                DisplayOrder = 1,
                CreatedAtUtc = DateTime.UtcNow
            },
            new SubscriptionPlan
            {
                Id = 2,
                Name = "Professional",
                Description = "For growing teams and businesses",
                Price = 299.00m,
                Currency = "EGP",
                BillingCycle = BillingCycle.Monthly,
                TrialDurationDays = 14,
                Features = "[\"5 Users\",\"50 GB Storage\",\"Priority Support\",\"Advanced Analytics\"]",
                MaxUsers = 5,
                MaxStorageGb = 50,
                PointsAwarded = 500,
                IsActive = true,
                DisplayOrder = 2,
                CreatedAtUtc = DateTime.UtcNow
            },
            new SubscriptionPlan
            {
                Id = 3,
                Name = "Enterprise",
                Description = "For large organizations with advanced needs",
                Price = 999.00m,
                Currency = "EGP",
                BillingCycle = BillingCycle.Monthly,
                TrialDurationDays = 30,
                Features = "[\"Unlimited Users\",\"Unlimited Storage\",\"24/7 Support\",\"Custom Integration\",\"Dedicated Account Manager\"]",
                MaxUsers = -1,
                MaxStorageGb = -1,
                PointsAwarded = 2000,
                IsActive = true,
                DisplayOrder = 3,
                CreatedAtUtc = DateTime.UtcNow
            }
        );
    }
}
