// ============================================
// File: DevPioneers.Domain/Entities/UserSubscription.cs
// ============================================
using DevPioneers.Domain.Common;
using DevPioneers.Domain.Enums;

namespace DevPioneers.Domain.Entities;

/// <summary>
/// User subscription entity
/// </summary>
public class UserSubscription : AuditableEntity
{
    /// <summary>
    /// User ID
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Navigation: User
    /// </summary>
    public virtual User User { get; set; } = null!;

    /// <summary>
    /// Subscription plan ID
    /// </summary>
    public int SubscriptionPlanId { get; set; }

    /// <summary>
    /// Navigation: Subscription plan
    /// </summary>
    public virtual SubscriptionPlan SubscriptionPlan { get; set; } = null!;

    /// <summary>
    /// Payment ID that activated this subscription
    /// </summary>
    public int? PaymentId { get; set; }

    /// <summary>
    /// Navigation: Payment
    /// </summary>
    public virtual Payment? Payment { get; set; }

    /// <summary>
    /// Subscription status
    /// </summary>
    public SubscriptionStatus Status { get; set; }

    /// <summary>
    /// Start date
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// End date
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Trial end date (if applicable)
    /// </summary>
    public DateTime? TrialEndDate { get; set; }

    /// <summary>
    /// Next billing date
    /// </summary>
    public DateTime? NextBillingDate { get; set; }

    /// <summary>
    /// Cancelled at
    /// </summary>
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Cancellation reason
    /// </summary>
    public string? CancellationReason { get; set; }

    /// <summary>
    /// Auto-renewal enabled
    /// </summary>
    public bool AutoRenewal { get; set; } = true;

    /// <summary>
    /// Check if subscription is active
    /// </summary>
    public bool IsActive()
    {
        return Status == SubscriptionStatus.Active &&
               DateTime.UtcNow >= StartDate &&
               DateTime.UtcNow <= EndDate;
    }

    /// <summary>
    /// Check if subscription is in trial
    /// </summary>
    public bool IsInTrial()
    {
        return Status == SubscriptionStatus.Trial &&
               TrialEndDate.HasValue &&
               DateTime.UtcNow <= TrialEndDate.Value;
    }

    /// <summary>
    /// Check if subscription is expired
    /// </summary>
    public bool IsExpired()
    {
        return DateTime.UtcNow > EndDate;
    }

    /// <summary>
    /// Check if subscription is in grace period
    /// </summary>
    public bool IsInGracePeriod()
    {
        return Status == SubscriptionStatus.GracePeriod;
    }

    /// <summary>
    /// Calculate days remaining
    /// </summary>
    public int DaysRemaining()
    {
        if (IsExpired()) return 0;
        return (EndDate - DateTime.UtcNow).Days;
    }

    /// <summary>
    /// Cancel subscription
    /// </summary>
    public void Cancel(string? reason = null)
    {
        Status = SubscriptionStatus.Cancelled;
        CancelledAt = DateTime.UtcNow;
        CancellationReason = reason;
        AutoRenewal = false;
    }

    /// <summary>
    /// Renew subscription
    /// </summary>
    public void Renew(DateTime newEndDate)
    {
        Status = SubscriptionStatus.Active;
        EndDate = newEndDate;
        NextBillingDate = newEndDate;
    }
}
