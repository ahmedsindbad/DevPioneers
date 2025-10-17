// ============================================
// File: DevPioneers.Application/Features/Subscriptions/DTOs/SubscriptionHistoryDto.cs
// ============================================
using DevPioneers.Application.Common.Models;

namespace DevPioneers.Application.Features.Subscriptions.DTOs;

public class SubscriptionHistoryDto : BaseDto
{
    public string PlanName { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string BillingCycle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }
    public int? PaymentId { get; set; }
    public bool AutoRenewal { get; set; }
 
    // Calculated properties
    public int DurationDays => (EndDate.Date - StartDate.Date).Days;
    public bool WasCancelled => CancelledAt.HasValue;
}
