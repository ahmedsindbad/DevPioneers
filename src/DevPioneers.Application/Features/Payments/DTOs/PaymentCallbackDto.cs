// ============================================
// File: DevPioneers.Application/Features/Payments/DTOs/PaymentCallbackDto.cs
// ============================================
namespace DevPioneers.Application.Features.Payments.DTOs;

public class PaymentCallbackDto
{
    public string PaymobOrderId { get; set; } = string.Empty;
    public string PaymobTransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Signature { get; set; }
    public Dictionary<string, object>? AdditionalData { get; set; }
}
