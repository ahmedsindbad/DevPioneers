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
    /// Configure global query filter for soft delete
    /// Automatically filters out deleted entities
    /// </summary>
    private void ConfigureGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        // DISABLED for initial migration to avoid EF Core issues
        // TODO: Enable after first successful migration
        // Uncomment the code below after database is created
        
        return; // Early return - filter is disabled
        
        /*
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            // Check if entity inherits from BaseEntity
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                // Create filter expression: entity => !entity.IsDeleted
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var filter = Expression.Lambda(Expression.Not(property), parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
        */
    }

    /// <summary>
    /// Configure decimal precision for money/currency fields
    /// Sets precision to (18, 2) for all decimal properties
    /// </summary>
    private void ConfigureDecimalPrecision(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(decimal) || property.ClrType == typeof(decimal?))
                {
                    property.SetPrecision(18);
                    property.SetScale(2);
                }
            }
        }
    }

    // ============================================
    // SaveChanges Override for Audit Trail
    // ============================================

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update timestamps before saving
        UpdateTimestamps();

        // Save changes (audit interceptor will handle audit trail)
        return await base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        UpdateTimestamps();
        return base.SaveChanges();
    }

    /// <summary>
    /// Update CreatedAt and UpdatedAt timestamps
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = ChangeTracker.Entries<BaseEntity>();

        foreach (var entry in entries)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAtUtc = DateTime.UtcNow;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAtUtc = DateTime.UtcNow;
                    break;

                case EntityState.Deleted:
                    // Soft delete: mark as deleted instead of removing
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAtUtc = DateTime.UtcNow;
                    break;
            }
        }
    }

    // ============================================
    // Helper Methods for Raw SQL
    // ============================================

    /// <summary>
    /// Execute raw SQL command
    /// </summary>
    public async Task<int> ExecuteSqlRawAsync(string sql, CancellationToken cancellationToken = default)
    {
        return await Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    /// <summary>
    /// Execute raw SQL query and return list
    /// </summary>
    public async Task<List<T>> ExecuteSqlQueryAsync<T>(string sql, CancellationToken cancellationToken = default) where T : class
    {
        return await Set<T>().FromSqlRaw(sql).ToListAsync(cancellationToken);
    }
}

// Required for LINQ expressions
