// ============================================
// File: DevPioneers.Domain/Entities/WalletTransaction.cs
// ============================================
using DevPioneers.Domain.Common;
using DevPioneers.Domain.Enums;
using DevPioneers.Domain.ValueObjects;

namespace DevPioneers.Domain.Entities;

/// <summary>
/// Wallet transaction entity
/// </summary>
public class WalletTransaction : AuditableEntity
{
    /// <summary>
    /// Wallet ID
    /// </summary>
    public int WalletId { get; set; }

    /// <summary>
    /// Navigation: Wallet
    /// </summary>
    public virtual Wallet Wallet { get; set; } = null!;

    /// <summary>
    /// Transaction type
    /// </summary>
    public TransactionType Type { get; set; }

    /// <summary>
    /// Amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = "EGP";

    /// <summary>
    /// Points (if applicable)
    /// </summary>
    public int? Points { get; set; }

    /// <summary>
    /// Balance before transaction
    /// </summary>
    public decimal BalanceBefore { get; set; }

    /// <summary>
    /// Balance after transaction
    /// </summary>
    public decimal BalanceAfter { get; set; }

    /// <summary>
    /// Transaction description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Reference to related entity (e.g., Payment ID, Subscription ID)
    /// </summary>
    public int? RelatedEntityId { get; set; }

    /// <summary>
    /// Related entity type
    /// </summary>
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// Transfer to user ID (if transfer transaction)
    /// </summary>
    public int? TransferToUserId { get; set; }

    /// <summary>
    /// Metadata (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Check if transaction is credit
    /// </summary>
    public bool IsCredit() => Type == TransactionType.Credit || Type == TransactionType.Refund || Type == TransactionType.PointsReward;

    /// <summary>
    /// Check if transaction is debit
    /// </summary>
    public bool IsDebit() => Type == TransactionType.Debit || Type == TransactionType.Transfer || Type == TransactionType.SubscriptionPayment;

    /// <summary>
    /// Get amount as Money value object
    /// </summary>
    public Money GetAmount() => new Money(Amount, Currency);
}