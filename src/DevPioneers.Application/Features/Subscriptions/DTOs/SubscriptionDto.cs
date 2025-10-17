// ============================================
// File: DevPioneers.Application/Features/Subscriptions/DTOs/SubscriptionDto.cs
// ============================================
using DevPioneers.Application.Common.Models;

namespace DevPioneers.Application.Features.Subscriptions.DTOs;

public class SubscriptionDto : BaseDto
{
    public int UserId { get; set; }
    public int PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public decimal PlanPrice { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? TrialEndDate { get; set; }
    public DateTime? NextBillingDate { get; set; }
    public bool AutoRenewal { get; set; }
    public int? PaymentId { get; set; }
 
    // Calculated properties
    public int DaysRemaining { get; set; }
    public bool IsExpiringSoon { get; set; }
    public bool IsActive => Status == "Active" || Status == "Trial";
    public bool IsTrial => Status == "Trial";
    public bool IsExpired => Status == "Expired";
    public bool IsCancelled => Status == "Cancelled";
}
