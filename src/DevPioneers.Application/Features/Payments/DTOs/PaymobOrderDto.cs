// ============================================
// File: DevPioneers.Application/Features/Payments/DTOs/PaymobOrderDto.cs
// ============================================
namespace DevPioneers.Application.Features.Payments.DTOs;

public class PaymobOrderDto
{
    public int PaymentId { get; set; }
    public string PaymobOrderId { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Calculated properties
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public TimeSpan TimeUntilExpiry => ExpiresAt - DateTime.UtcNow;
    public int MinutesUntilExpiry => Math.Max(0, (int)TimeUntilExpiry.TotalMinutes);

    // Display properties
    public string AmountDisplay => $"{Amount:C} {Currency}";
    public string ExpiryDisplay => IsExpired ? "Expired" : $"Expires in {MinutesUntilExpiry} minutes";
}
