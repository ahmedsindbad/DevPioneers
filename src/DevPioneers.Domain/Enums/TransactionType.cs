// ============================================
// File: DevPioneers.Domain/Enums/TransactionType.cs
// ============================================
namespace DevPioneers.Domain.Enums;

/// <summary>
/// Wallet transaction type
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Credit (add funds)
    /// </summary>
    Credit = 1,

    /// <summary>
    /// Debit (remove funds)
    /// </summary>
    Debit = 2,

    /// <summary>
    /// Transfer to another user
    /// </summary>
    Transfer = 3,

    /// <summary>
    /// Refund
    /// </summary>
    Refund = 4,

    /// <summary>
    /// Subscription payment
    /// </summary>
    SubscriptionPayment = 5,

    /// <summary>
    /// Points reward
    /// </summary>
    PointsReward = 6,

    /// <summary>
    /// Points redemption
    /// </summary>
    PointsRedemption = 7,

    /// <summary>
    /// Adjustment by admin
    /// </summary>
    Adjustment = 8
}
