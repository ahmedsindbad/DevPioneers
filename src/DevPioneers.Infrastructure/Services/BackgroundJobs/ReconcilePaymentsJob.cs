// ============================================
// File: DevPioneers.Infrastructure/Services/BackgroundJobs/ReconcilePaymentsJob.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Background job to reconcile pending/processing payments with payment gateway
/// </summary>
public class ReconcilePaymentsJob : IReconcilePaymentsJob
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly ILogger<ReconcilePaymentsJob> _logger;

    public ReconcilePaymentsJob(
        IApplicationDbContext context,
        IPaymentService paymentService,
        ILogger<ReconcilePaymentsJob> logger)
    {
        _context = context;
        _paymentService = paymentService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ReconcilePaymentsJob at {Time}", DateTime.UtcNow);

        try
        {
            // Find pending/processing payments older than 30 minutes
            var cutoffTime = DateTime.UtcNow.AddMinutes(-30);
            var paymentsToReconcile = await _context.Payments
                .Where(p => (p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.Processing) &&
                           p.CreatedAtUtc < cutoffTime &&
                           !string.IsNullOrEmpty(p.PaymobOrderId))
                .ToListAsync(cancellationToken);

            if (!paymentsToReconcile.Any())
            {
                _logger.LogInformation("No payments found to reconcile");
                return;
            }

            _logger.LogInformation("Found {Count} payments to reconcile", paymentsToReconcile.Count);

            var reconciledCount = 0;
            var failedCount = 0;
            var errorCount = 0;

            foreach (var payment in paymentsToReconcile)
            {
                try
                {
                    _logger.LogInformation(
                        "Checking payment status for PaymentId: {PaymentId}, PaymobOrderId: {PaymobOrderId}",
                        payment.Id,
                        payment.PaymobOrderId);

                    // Get payment status from Paymob
                    var statusResult = await _paymentService.GetPaymentStatusAsync(
                        payment.PaymobOrderId!,
                        cancellationToken);

                    if (statusResult.Status.Equals("completed", StringComparison.OrdinalIgnoreCase) ||
                        statusResult.Status.Equals("success", StringComparison.OrdinalIgnoreCase))
                    {
                        // Mark as completed
                        payment.MarkAsCompleted();
                        reconciledCount++;

                        _logger.LogInformation(
                            "Payment {PaymentId} reconciled as COMPLETED",
                            payment.Id);
                    }
                    else if (statusResult.Status.Equals("failed", StringComparison.OrdinalIgnoreCase) ||
                             statusResult.Status.Equals("expired", StringComparison.OrdinalIgnoreCase) ||
                             statusResult.Status.Equals("cancelled", StringComparison.OrdinalIgnoreCase))
                    {
                        // Mark as failed
                        payment.MarkAsFailed($"Payment gateway status: {statusResult.Status}");
                        failedCount++;

                        _logger.LogWarning(
                            "Payment {PaymentId} reconciled as FAILED with status: {Status}",
                            payment.Id,
                            statusResult.Status);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Payment {PaymentId} still in status: {Status}. Skipping for now.",
                            payment.Id,
                            statusResult.Status);
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(
                        ex,
                        "Error reconciling payment {PaymentId}, PaymobOrderId: {PaymobOrderId}",
                        payment.Id,
                        payment.PaymobOrderId);
                }
            }

            // Save all changes at once
            if (reconciledCount > 0 || failedCount > 0)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogInformation(
                "ReconcilePaymentsJob completed. Reconciled: {ReconciledCount}, Failed: {FailedCount}, Errors: {ErrorCount}",
                reconciledCount,
                failedCount,
                errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in ReconcilePaymentsJob");
            throw;
        }
    }
}
