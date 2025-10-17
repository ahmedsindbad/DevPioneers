// ============================================
// File: DevPioneers.Application/Features/Payments/DTOs/PaymentDto.cs
// ============================================
using DevPioneers.Application.Common.Models;

namespace DevPioneers.Application.Features.Payments.DTOs;

public class PaymentDto : BaseDto
{
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PaymobOrderId { get; set; }
    public string? PaymobTransactionId { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public string? FailureReason { get; set; }
    public DateTime? RefundedAt { get; set; }
    public decimal? RefundAmount { get; set; }
    public string? RefundReason { get; set; }
    public int? SubscriptionPlanId { get; set; }
    public string? SubscriptionPlanName { get; set; }
    // Calculated properties
    public bool IsCompleted => Status == "Completed";
    public bool IsPending => Status == "Pending" || Status == "Processing";
    public bool IsFailed => Status == "Failed" || Status == "Cancelled" || Status == "Expired";
    public bool IsRefunded => Status == "Refunded" || Status == "PartiallyRefunded";
    public bool HasRefund => RefundedAt.HasValue && RefundAmount.HasValue;

    // Display properties
    public string AmountDisplay => $"{Amount:C} {Currency}";
    public string RefundDisplay => HasRefund ? $"{RefundAmount:C} {Currency}" : string.Empty;
}
