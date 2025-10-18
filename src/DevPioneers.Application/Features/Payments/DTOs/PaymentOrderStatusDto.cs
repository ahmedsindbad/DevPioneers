// ============================================
// File: DevPioneers.Application/Features/Payments/DTOs/PaymentOrderStatusDto.cs
// ============================================
namespace DevPioneers.Application.Features.Payments.DTOs;

/// <summary>
/// DTO for payment order status information
/// Contains details about the payment order and its current status from Paymob
/// </summary>
public class PaymentOrderStatusDto
{
    /// <summary>
    /// Internal payment ID in our system
    /// </summary>
    public int PaymentId { get; set; }

    /// <summary>
    /// Paymob order ID
    /// </summary>
    public string PaymobOrderId { get; set; } = string.Empty;

    /// <summary>
    /// Payment status (pending, processing, completed, failed, etc.)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency code (EGP, USD, etc.)
    /// </summary>
    public string Currency { get; set; } = "EGP";

    /// <summary>
    /// Payment description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// When the payment order was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the payment was completed (if applicable)
    /// </summary>
    public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Name of the subscription plan (if payment is for subscription)
    /// </summary>
    public string? SubscriptionPlanName { get; set; }

    /// <summary>
    /// Error message if payment failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Payment URL for frontend redirection (if still pending)
    /// </summary>
    public string? PaymentUrl { get; set; }

    /// <summary>
    /// Payment expiration time (usually 30 minutes from creation)
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Indicates if the payment order has expired
    /// </summary>
    public bool IsExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    /// <summary>
    /// Indicates if the payment is in a final state (completed or failed)
    /// </summary>
    public bool IsFinal => Status.ToLowerInvariant() is "completed" or "failed" or "cancelled" or "expired";

    /// <summary>
    /// Indicates if the payment was successful
    /// </summary>
    public bool IsSuccessful => Status.ToLowerInvariant() is "completed" or "paid" or "success";

    /// <summary>
    /// User-friendly status message
    /// </summary>
    public string StatusMessage => Status.ToLowerInvariant() switch
    {
        "pending" => "Payment is pending - waiting for user action",
        "processing" => "Payment is being processed",
        "completed" or "paid" or "success" => "Payment completed successfully",
        "failed" => $"Payment failed{(string.IsNullOrEmpty(ErrorMessage) ? "" : $": {ErrorMessage}")}",
        "cancelled" => "Payment was cancelled",
        "expired" => "Payment order expired",
        _ => $"Payment status: {Status}"
    };
}