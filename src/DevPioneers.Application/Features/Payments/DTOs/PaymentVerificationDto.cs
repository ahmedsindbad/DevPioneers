// ============================================
// File: DevPioneers.Application/Features/Payments/DTOs/PaymentVerificationDto.cs
// ============================================
namespace DevPioneers.Application.Features.Payments.DTOs;

public class PaymentVerificationDto
{
    public int PaymentId { get; set; }
    public string PaymobOrderId { get; set; } = string.Empty;
    public string? PaymobTransactionId { get; set; }
    public bool IsSuccess { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime VerifiedAt { get; set; }
 
    // Display properties
    public string AmountDisplay => $"{Amount:C} {Currency}";
    public string ResultDisplay => IsSuccess ? "✅ Payment Successful" : "❌ Payment Failed";
}
