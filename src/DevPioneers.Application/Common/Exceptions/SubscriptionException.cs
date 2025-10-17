// ============================================
// File: DevPioneers.Application/Common/Exceptions/SubscriptionException.cs
// ============================================
namespace DevPioneers.Application.Common.Exceptions;

/// <summary>
/// Exception specific to subscription operations
/// </summary>
public class SubscriptionException : Exception
{
    public SubscriptionException()
        : base("A subscription error occurred.")
    {
    }

    public SubscriptionException(string message)
        : base(message)
    {
    }

    public SubscriptionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public SubscriptionException(string operation, int subscriptionId, string reason)
        : base($"Subscription {operation} failed for subscription {subscriptionId}: {reason}")
    {
        Operation = operation;
        SubscriptionId = subscriptionId;
        Reason = reason;
    }

    public string? Operation { get; }
    public int? SubscriptionId { get; }
    public int? UserId { get; set; }
    public int? PlanId { get; set; }
    public string? Reason { get; }
}