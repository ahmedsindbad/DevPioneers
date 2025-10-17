// ============================================
// File: DevPioneers.Persistence/Contexts/ApplicationDbContext.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Domain.Common;
using DevPioneers.Domain.Entities;
using DevPioneers.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
using System.Linq.Expressions;

namespace DevPioneers.Persistence.Contexts;

/// <summary>
/// Main database context for DevPioneers application
/// Implements Clean Architecture pattern with EF Core 9
/// </summary>
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly AuditInterceptor _auditInterceptor;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        AuditInterceptor auditInterceptor)
        : base(options)
    {
        _auditInterceptor = auditInterceptor;
    }

    // ============================================
    // DbSets - Entity Collections
    // ============================================

    #region User Management
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<OtpCode> OtpCodes => Set<OtpCode>();
    #endregion

    #region Subscription & Billing
    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();
    public DbSet<UserSubscription> UserSubscriptions => Set<UserSubscription>();
    public DbSet<Payment> Payments => Set<Payment>();
    #endregion

    #region Wallet & Points
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    #endregion

    #region Audit
    public DbSet<AuditTrail> AuditTrails => Set<AuditTrail>();
    #endregion

    // ============================================
    // Configuration
    // ============================================

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Add audit interceptor
        optionsBuilder.AddInterceptors(_auditInterceptor);

        // Suppress pending model changes warning for initial migration
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));

        // Enable detailed errors in development
#if DEBUG
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();
#endif
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all entity configurations from current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure soft delete global query filter
        ConfigureGlobalQueryFilters(modelBuilder);

        // Configure decimal precision globally
        ConfigureDecimalPrecision(modelBuilder);
    }

    // ============================================
    // Global Configurations
    // ============================================

    /// <summary>
    /// Configure global query filters for soft delete
    /// </summary>
    private void ConfigureGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        // Get all entity types that inherit from BaseEntity
        var entityTypes = modelBuilder.Model
            .GetEntityTypes()
            .Where(e => typeof(BaseEntity).IsAssignableFrom(e.ClrType))
            .ToList();

        foreach (var entityType in entityTypes)
        {
            // Create lambda expression: entity => !entity.IsDeleted
            var parameter = Expression.Parameter(entityType.ClrType, "entity");
            var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
            var condition = Expression.Equal(property, Expression.Constant(false));
            var lambda = Expression.Lambda(condition, parameter);

            // Apply the filter
            modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
        }
    }

    /// <summary>
    /// Configure decimal precision globally for money fields
    /// </summary>
    private void ConfigureDecimalPrecision(ModelBuilder modelBuilder)
    {
        foreach (var property in modelBuilder.Model.GetEntityTypes()
            .SelectMany(t => t.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
        {
            // Set precision to 18,2 for all decimal properties
            // This covers money, prices, amounts, etc.
            property.SetColumnType("decimal(18,2)");
        }
    }

    /// <summary>
    /// Override SaveChanges to handle audit and soft delete
    /// </summary>
    public override int SaveChanges()
    {
        ProcessSoftDeletes();
        UpdateTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// Override SaveChangesAsync to handle audit and soft delete
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ProcessSoftDeletes();
        UpdateTimestamps();
        return await base.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Process soft deletes - convert Delete operations to IsDeleted=true
    /// </summary>
    private void ProcessSoftDeletes()
    {
        var deletedEntries = ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Deleted)
            .ToList();

        foreach (var entry in deletedEntries)
        {
            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedAtUtc = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Update CreatedAtUtc and UpdatedAtUtc timestamps
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker
            .Entries<BaseEntity>()
            .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified)
            .ToList();

        var utcNow = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = utcNow;
                    entry.Entity.UpdatedAtUtc = utcNow;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = utcNow;
                    // Prevent CreatedAtUtc from being modified
                    entry.Property(nameof(BaseEntity.CreatedAtUtc)).IsModified = false;
                    break;
            }
        }
    }
}