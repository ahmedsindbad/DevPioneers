// ============================================
// File: DevPioneers.Application/Features/Subscriptions/DTOs/ExpiringSubscriptionDto.cs
// ============================================
namespace DevPioneers.Application.Features.Subscriptions.DTOs;

public class ExpiringSubscriptionDto
{
    public int SubscriptionId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string PlanName { get; set; } = string.Empty;
    public DateTime EndDate { get; set; }
    public int DaysUntilExpiry { get; set; }
    public bool AutoRenewal { get; set; }
    public string Status { get; set; } = string.Empty;
}