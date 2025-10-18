// File: DevPioneers.Application/Features/Subscriptions/DTOs/SubscriptionAnalyticsDto.cs
// ============================================
namespace DevPioneers.Application.Features.Subscriptions.DTOs;

public class SubscriptionAnalyticsDto
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public int TotalSubscriptions { get; set; }
    public int ActiveSubscriptions { get; set; }
    public int TrialSubscriptions { get; set; }
    public int CancelledSubscriptions { get; set; }
    public int ExpiredSubscriptions { get; set; }
    public decimal TotalRevenue { get; set; }
    public List<SubscriptionPlanAnalyticsDto> PlanBreakdown { get; set; } = new();
}

public class SubscriptionPlanAnalyticsDto
{
    public int PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Revenue { get; set; }
}