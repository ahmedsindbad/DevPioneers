// ============================================
// File: DevPioneers.Application/Features/Subscriptions/DTOs/SubscriptionPlanDto.cs
// ============================================
using DevPioneers.Application.Common.Models;

namespace DevPioneers.Application.Features.Subscriptions.DTOs;

public class SubscriptionPlanDto : BaseDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = string.Empty;
    public int TrialDurationDays { get; set; }
    public string Features { get; set; } = string.Empty;
    public int MaxUsers { get; set; }
    public int MaxStorageGb { get; set; }
    public int PointsAwarded { get; set; }
    public bool IsActive { get; set; }
    public int DisplayOrder { get; set; }
    public decimal DiscountPercentage { get; set; }
 
    // Calculated properties
    public decimal DiscountedPrice => Price * (1 - DiscountPercentage / 100);
    public bool HasDiscount => DiscountPercentage > 0;
    public bool HasTrial => TrialDurationDays > 0;
    public bool IsUnlimited => MaxUsers == -1 || MaxStorageGb == -1;
}
