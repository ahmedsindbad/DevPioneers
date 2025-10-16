// ============================================
// File: DevPioneers.Domain/Entities/SubscriptionPlan.cs
// ============================================
using DevPioneers.Domain.Common;
using DevPioneers.Domain.Enums;
using DevPioneers.Domain.ValueObjects;

namespace DevPioneers.Domain.Entities;

/// <summary>
/// Subscription plan entity
/// </summary>
public class SubscriptionPlan : AuditableEntity
{
    /// <summary>
    /// Plan name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Plan description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Plan price
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = "EGP";

    /// <summary>
    /// Billing cycle
    /// </summary>
    public BillingCycle BillingCycle { get; set; }

    /// <summary>
    /// Trial duration in days (0 = no trial)
    /// </summary>
    public int TrialDurationDays { get; set; } = 0;

    /// <summary>
    /// Features (JSON serialized)
    /// </summary>
    public string Features { get; set; } = "[]";

    /// <summary>
    /// Max users allowed (-1 = unlimited)
    /// </summary>
    public int MaxUsers { get; set; } = 1;

    /// <summary>
    /// Max storage in GB (-1 = unlimited)
    /// </summary>
    public int MaxStorageGb { get; set; } = 10;

    /// <summary>
    /// Points awarded on subscription
    /// </summary>
    public int PointsAwarded { get; set; } = 0;

    /// <summary>
    /// Is plan active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Display order
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Discount percentage (0-100)
    /// </summary>
    public decimal DiscountPercentage { get; set; } = 0;

    /// <summary>
    /// Navigation: User subscriptions
    /// </summary>
    public virtual ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();

    /// <summary>
    /// Calculate effective price after discount
    /// </summary>
    public Money GetEffectivePrice()
    {
        decimal effectivePrice = Price * (1 - (DiscountPercentage / 100));
        return new Money(effectivePrice, Currency);
    }

    /// <summary>
    /// Check if plan has trial
    /// </summary>
    public bool HasTrial() => TrialDurationDays > 0;

    /// <summary>
    /// Get plan duration in months
    /// </summary>
    public int GetDurationInMonths()
    {
        return BillingCycle switch
        {
            BillingCycle.Monthly => 1,
            BillingCycle.Quarterly => 3,
            BillingCycle.SemiAnnual => 6,
            BillingCycle.Annual => 12,
            BillingCycle.Lifetime => 0,
            _ => 1
        };
    }
}