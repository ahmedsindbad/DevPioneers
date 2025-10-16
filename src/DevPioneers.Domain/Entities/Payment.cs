// ============================================
// File: DevPioneers.Domain/Entities/Payment.cs
// ============================================
using DevPioneers.Domain.Common;
using DevPioneers.Domain.Enums;
using DevPioneers.Domain.ValueObjects;

namespace DevPioneers.Domain.Entities;

/// <summary>
/// Payment transaction entity
/// </summary>
public class Payment : AuditableEntity
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
    /// Subscription plan ID (if payment is for subscription)
    /// </summary>
    public int? SubscriptionPlanId { get; set; }

    /// <summary>
    /// Navigation: Subscription plan
    /// </summary>
    public virtual SubscriptionPlan? SubscriptionPlan { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = "EGP";

    /// <summary>
    /// Payment status
    /// </summary>
    public PaymentStatus Status { get; set; }

    /// <summary>
    /// Payment method
    /// </summary>
    public PaymentMethod PaymentMethod { get; set; }

    /// <summary>
    /// Paymob order ID
    /// </summary>
    public string? PaymobOrderId { get; set; }

    /// <summary>
    /// Paymob transaction ID
    /// </summary>
    public string? PaymobTransactionId { get; set; }

    /// <summary>
    /// Payment gateway reference
    /// </summary>
    public string? GatewayReference { get; set; }

    /// <summary>
    /// Payment description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Paid at (completion timestamp)
    /// </summary>
    public DateTime? PaidAt { get; set; }

    /// <summary>
    /// Failed at (failure timestamp)
    /// </summary>
    public DateTime? FailedAt { get; set; }

    /// <summary>
    /// Failure reason
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Refunded at
    /// </summary>
    public DateTime? RefundedAt { get; set; }

    /// <summary>
    /// Refund amount
    /// </summary>
    public decimal? RefundAmount { get; set; }

    /// <summary>
    /// Refund reason
    /// </summary>
    public string? RefundReason { get; set; }

    /// <summary>
    /// IP address of payer
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User agent
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Metadata (JSON)
    /// </summary>
    public string? Metadata { get; set; }

    /// <summary>
    /// Navigation: User subscriptions created from this payment
    /// </summary>
    public virtual ICollection<UserSubscription> Subscriptions { get; set; } = new List<UserSubscription>();

    /// <summary>
    /// Check if payment is completed
    /// </summary>
    public bool IsCompleted() => Status == PaymentStatus.Completed;

    /// <summary>
    /// Check if payment is pending
    /// </summary>
    public bool IsPending() => Status == PaymentStatus.Pending || Status == PaymentStatus.Processing;

    /// <summary>
    /// Check if payment failed
    /// </summary>
    public bool IsFailed() => Status == PaymentStatus.Failed;

    /// <summary>
    /// Mark payment as completed
    /// </summary>
    public void MarkAsCompleted(string? transactionId = null)
    {
        Status = PaymentStatus.Completed;
        PaidAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(transactionId))
            PaymobTransactionId = transactionId;
    }

    /// <summary>
    /// Mark payment as failed
    /// </summary>
    public void MarkAsFailed(string reason)
    {
        Status = PaymentStatus.Failed;
        FailedAt = DateTime.UtcNow;
        FailureReason = reason;
    }

    /// <summary>
    /// Process refund
    /// </summary>
    public void ProcessRefund(decimal amount, string? reason = null)
    {
        if (amount > Amount)
            throw new InvalidOperationException("Refund amount cannot exceed payment amount");

        RefundAmount = amount;
        RefundedAt = DateTime.UtcNow;
        RefundReason = reason;
        Status = amount == Amount ? PaymentStatus.Refunded : PaymentStatus.PartiallyRefunded;
    }

    /// <summary>
    /// Get payment amount as Money value object
    /// </summary>
    public Money GetAmount() => new Money(Amount, Currency);
}