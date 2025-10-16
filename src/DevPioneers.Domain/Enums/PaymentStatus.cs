// ============================================
// File: DevPioneers.Domain/Enums/PaymentStatus.cs
// ============================================
namespace DevPioneers.Domain.Enums;

/// <summary>
/// Payment transaction status
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment initiated but not completed
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Payment processing in progress
    /// </summary>
    Processing = 1,

    /// <summary>
    /// Payment completed successfully
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Payment failed
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Payment cancelled by user
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Payment refunded
    /// </summary>
    Refunded = 5,

    /// <summary>
    /// Partial refund
    /// </summary>
    PartiallyRefunded = 6,

    /// <summary>
    /// Payment expired (timeout)
    /// </summary>
    Expired = 7
}
