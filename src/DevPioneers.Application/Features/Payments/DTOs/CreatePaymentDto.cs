// ============================================
// File: DevPioneers.Application/Features/Payments/DTOs/CreatePaymentDto.cs
// ============================================
namespace DevPioneers.Application.Features.Payments.DTOs;

public class CreatePaymentDto
{
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "EGP";
    public string Description { get; set; } = string.Empty;
    public int? SubscriptionPlanId { get; set; }
    public string PaymentMethod { get; set; } = "CreditCard";
}
