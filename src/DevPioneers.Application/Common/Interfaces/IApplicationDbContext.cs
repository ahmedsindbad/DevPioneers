// ============================================
// File: DevPioneers.Application/Common/Interfaces/IApplicationDbContext.cs
// ============================================
using DevPioneers.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace DevPioneers.Application.Common.Interfaces;

/// <summary>
/// Database context interface for Application layer
/// Follows Dependency Inversion Principle
/// </summary>
public interface IApplicationDbContext
{
    // User Management
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<OtpCode> OtpCodes { get; }

    // Subscription & Billing
    DbSet<SubscriptionPlan> SubscriptionPlans { get; }
    DbSet<UserSubscription> UserSubscriptions { get; }
    DbSet<Payment> Payments { get; }

    // Wallet & Points
    DbSet<Wallet> Wallets { get; }
    DbSet<WalletTransaction> WalletTransactions { get; }

    // Audit
    DbSet<AuditTrail> AuditTrails { get; }

    // SaveChanges methods
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    int SaveChanges();
}