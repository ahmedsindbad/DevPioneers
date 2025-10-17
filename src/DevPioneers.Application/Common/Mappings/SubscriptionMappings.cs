// ============================================
// File: DevPioneers.Application/Common/Mappings/SubscriptionMappings.cs
// ============================================
using DevPioneers.Application.Features.Subscriptions.DTOs;
using DevPioneers.Domain.Entities;

namespace DevPioneers.Application.Common.Mappings;

public static class SubscriptionMappings
{
    public static SubscriptionDto ToDto(this UserSubscription subscription)
    {
        return new SubscriptionDto
        {
            Id = subscription.Id,
            UserId = subscription.UserId,
            PlanId = subscription.SubscriptionPlanId,
            Status = subscription.Status.ToString(),
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            TrialEndDate = subscription.TrialEndDate,
            NextBillingDate = subscription.NextBillingDate,
            AutoRenewal = subscription.AutoRenewal,
            PaymentId = subscription.PaymentId,
            CreatedAtUtc = subscription.CreatedAtUtc,
            UpdatedAtUtc = subscription.UpdatedAtUtc
        };
    }

    public static SubscriptionPlanDto ToDto(this SubscriptionPlan plan)
    {
        return new SubscriptionPlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            Price = plan.Price,
            Currency = plan.Currency,
            BillingCycle = plan.BillingCycle.ToString(),
            TrialDurationDays = plan.TrialDurationDays,
            Features = plan.Features,
            MaxUsers = plan.MaxUsers,
            MaxStorageGb = plan.MaxStorageGb,
            PointsAwarded = plan.PointsAwarded,
            IsActive = plan.IsActive,
            DisplayOrder = plan.DisplayOrder,
            DiscountPercentage = plan.DiscountPercentage,
            CreatedAtUtc = plan.CreatedAtUtc,
            UpdatedAtUtc = plan.UpdatedAtUtc
        };
    }
}
