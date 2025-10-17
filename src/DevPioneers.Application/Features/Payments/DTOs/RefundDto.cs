// ============================================
// File: DevPioneers.Application/Features/Payments/DTOs/RefundDto.cs
// ============================================
namespace DevPioneers.Application.Features.Payments.DTOs;

public class RefundDto
{
    public int PaymentId { get; set; }
    public string RefundId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public int ProcessedByUserId { get; set; }
 
    // Display properties
    public string AmountDisplay => $"{Amount:C} {Currency}";
}
