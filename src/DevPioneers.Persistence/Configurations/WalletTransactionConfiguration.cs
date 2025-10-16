// ============================================
// File: DevPioneers.Persistence/Configurations/WalletTransactionConfiguration.cs
// ============================================
using DevPioneers.Domain.Entities;
using DevPioneers.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevPioneers.Persistence.Configurations;

public class WalletTransactionConfiguration : IEntityTypeConfiguration<WalletTransaction>
{
    public void Configure(EntityTypeBuilder<WalletTransaction> builder)
    {
        // Table name
        builder.ToTable("WalletTransactions");

        // Primary key
        builder.HasKey(wt => wt.Id);

        // Properties
        builder.Property(wt => wt.Type)
            .HasConversion<string>()
            .HasMaxLength(30);

        builder.Property(wt => wt.Amount)
            .IsRequired()
            .HasPrecision(18, 2);

        builder.Property(wt => wt.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("EGP");

        builder.Property(wt => wt.BalanceBefore)
            .HasPrecision(18, 2);

        builder.Property(wt => wt.BalanceAfter)
            .HasPrecision(18, 2);

        builder.Property(wt => wt.Description)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(wt => wt.RelatedEntityType)
            .HasMaxLength(100);

        // Indexes
        builder.HasIndex(wt => new { wt.WalletId, wt.CreatedAtUtc })
            .HasDatabaseName("IX_WalletTransactions_WalletId_CreatedAt");

        builder.HasIndex(wt => wt.Type)
            .HasDatabaseName("IX_WalletTransactions_Type");

        builder.HasIndex(wt => new { wt.RelatedEntityType, wt.RelatedEntityId })
            .HasDatabaseName("IX_WalletTransactions_RelatedEntity")
            .HasFilter("[RelatedEntityId] IS NOT NULL");

        builder.HasIndex(wt => wt.TransferToUserId)
            .HasDatabaseName("IX_WalletTransactions_TransferToUserId")
            .HasFilter("[TransferToUserId] IS NOT NULL");
    }
}
