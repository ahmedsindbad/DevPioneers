// ============================================
// File: DevPioneers.Domain/Enums/SubscriptionStatus.cs
// ============================================
namespace DevPioneers.Domain.Enums;

/// <summary>
/// Subscription status
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>
    /// Subscription is in trial period
    /// </summary>
    Trial = 0,

    /// <summary>
    /// Subscription is active
    /// </summary>
    Active = 1,

    /// <summary>
    /// Subscription expired but within grace period
    /// </summary>
    GracePeriod = 2,

    /// <summary>
    /// Subscription expired
    /// </summary>
    Expired = 3,

    /// <summary>
    /// Subscription cancelled by user
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Subscription suspended (payment failed)
    /// </summary>
    Suspended = 5,

    /// <summary>
    /// Subscription pending payment
    /// </summary>
    PendingPayment = 6
}