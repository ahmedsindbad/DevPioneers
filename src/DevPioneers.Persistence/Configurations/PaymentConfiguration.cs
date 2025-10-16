// ============================================
// File: DevPioneers.Persistence/Configurations/PaymentConfiguration.cs
// ============================================
using DevPioneers.Domain.Entities;
using DevPioneers.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevPioneers.Persistence.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        // Table name
        builder.ToTable("Payments");

        // Primary key
        builder.HasKey(p => p.Id);

        // Properties
        builder.Property(p => p.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("EGP");

        builder.Property(p => p.Status)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(p => p.PaymentMethod)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(p => p.PaymobOrderId)
            .HasMaxLength(100);

        builder.Property(p => p.PaymobTransactionId)
            .HasMaxLength(100);

        builder.Property(p => p.GatewayReference)
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.FailureReason)
            .HasMaxLength(500);

        builder.Property(p => p.RefundAmount)
            .HasPrecision(18, 2);

        builder.Property(p => p.RefundReason)
            .HasMaxLength(500);

        builder.Property(p => p.IpAddress)
            .HasMaxLength(45);

        builder.Property(p => p.UserAgent)
            .HasMaxLength(500);

        // Indexes
        builder.HasIndex(p => p.PaymobOrderId)
            .HasDatabaseName("IX_Payments_PaymobOrderId")
            .HasFilter("[PaymobOrderId] IS NOT NULL");

        builder.HasIndex(p => p.PaymobTransactionId)
            .HasDatabaseName("IX_Payments_PaymobTransactionId")
            .HasFilter("[PaymobTransactionId] IS NOT NULL");

        builder.HasIndex(p => new { p.UserId, p.Status })
            .HasDatabaseName("IX_Payments_UserId_Status");

        builder.HasIndex(p => p.CreatedAtUtc)
            .HasDatabaseName("IX_Payments_CreatedAt");

        // Relationships
        builder.HasOne(p => p.SubscriptionPlan)
            .WithMany()
            .HasForeignKey(p => p.SubscriptionPlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
