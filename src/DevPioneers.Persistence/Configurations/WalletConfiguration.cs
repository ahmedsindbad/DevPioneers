// ============================================
// File: DevPioneers.Persistence/Configurations/WalletConfiguration.cs
// ============================================
using DevPioneers.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevPioneers.Persistence.Configurations;

public class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        // Table name
        builder.ToTable("Wallets");

        // Primary key
        builder.HasKey(w => w.Id);

        // Properties
        builder.Property(w => w.Balance)
            .IsRequired()
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(w => w.Currency)
            .IsRequired()
            .HasMaxLength(3)
            .HasDefaultValue("EGP");

        builder.Property(w => w.Points)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(w => w.TotalEarned)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(w => w.TotalSpent)
            .HasPrecision(18, 2)
            .HasDefaultValue(0);

        builder.Property(w => w.IsActive)
            .HasDefaultValue(true);

        // Indexes
        builder.HasIndex(w => w.UserId)
            .IsUnique()
            .HasDatabaseName("IX_Wallets_UserId");

        builder.HasIndex(w => w.IsActive)
            .HasDatabaseName("IX_Wallets_IsActive");

        // Relationships
        builder.HasMany(w => w.Transactions)
            .WithOne(wt => wt.Wallet)
            .HasForeignKey(wt => wt.WalletId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
