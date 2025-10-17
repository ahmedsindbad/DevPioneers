// ============================================
// File: DevPioneers.Application/Features/Auth/DTOs/SubscriptionSummaryDto.cs
// ============================================
namespace DevPioneers.Application.Features.Auth.DTOs;

public class SubscriptionSummaryDto
{
    public string PlanName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public int DaysRemaining => (EndDate.Date - DateTime.UtcNow.Date).Days;
    public bool IsExpiringSoon => DaysRemaining <= 7 && DaysRemaining > 0;
    public bool IsExpired => DaysRemaining < 0;
}
