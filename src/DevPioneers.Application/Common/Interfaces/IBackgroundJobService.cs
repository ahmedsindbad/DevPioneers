// ============================================
// File: DevPioneers.Application/Common/Interfaces/IBackgroundJobService.cs
// ============================================
namespace DevPioneers.Application.Common.Interfaces;

/// <summary>
/// Base interface for background job services
/// </summary>
public interface IBackgroundJobService
{
    /// <summary>
    /// Execute the background job
    /// </summary>
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Service for expiring subscriptions that have passed their end date
/// </summary>
public interface IExpireSubscriptionsJob : IBackgroundJobService
{
}

/// <summary>
/// Service for reconciling payment statuses with payment gateway
/// </summary>
public interface IReconcilePaymentsJob : IBackgroundJobService
{
}

/// <summary>
/// Service for sending queued emails
/// </summary>
public interface ISendEmailJob : IBackgroundJobService
{
}

/// <summary>
/// Service for cleaning old audit trail records
/// </summary>
public interface ICleanOldAuditTrailJob : IBackgroundJobService
{
}
