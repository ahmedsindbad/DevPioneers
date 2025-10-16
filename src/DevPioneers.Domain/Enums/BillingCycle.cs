// ============================================
// File: DevPioneers.Domain/Enums/BillingCycle.cs
// ============================================
namespace DevPioneers.Domain.Enums;

/// <summary>
/// Billing cycle for subscriptions
/// </summary>
public enum BillingCycle
{
    /// <summary>
    /// Monthly billing
    /// </summary>
    Monthly = 1,

    /// <summary>
    /// Quarterly billing (3 months)
    /// </summary>
    Quarterly = 3,

    /// <summary>
    /// Semi-annual billing (6 months)
    /// </summary>
    SemiAnnual = 6,

    /// <summary>
    /// Annual billing (12 months)
    /// </summary>
    Annual = 12,

    /// <summary>
    /// Lifetime (one-time payment)
    /// </summary>
    Lifetime = 0
}