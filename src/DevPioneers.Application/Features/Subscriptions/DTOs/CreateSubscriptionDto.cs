// ============================================
// File: DevPioneers.Application/Features/Subscriptions/DTOs/CreateSubscriptionDto.cs
// ============================================
namespace DevPioneers.Application.Features.Subscriptions.DTOs;

public class CreateSubscriptionDto
{
    public int UserId { get; set; }
    public int SubscriptionPlanId { get; set; }
    public int? PaymentId { get; set; }
    public bool AcceptTerms { get; set; }
}
