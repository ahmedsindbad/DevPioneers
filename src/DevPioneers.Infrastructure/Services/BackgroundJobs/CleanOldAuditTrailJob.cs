// ============================================
// File: DevPioneers.Infrastructure/Services/BackgroundJobs/CleanOldAuditTrailJob.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Background job to clean old audit trail records to maintain database performance
/// </summary>
public class CleanOldAuditTrailJob : ICleanOldAuditTrailJob
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CleanOldAuditTrailJob> _logger;

    // Configuration: Keep audit trails for 90 days by default
    private const int RetentionDays = 90;

    public CleanOldAuditTrailJob(
        IApplicationDbContext context,
        ILogger<CleanOldAuditTrailJob> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting CleanOldAuditTrailJob at {Time}", DateTime.UtcNow);

        try
        {
            // Calculate cutoff date
            var cutoffDate = DateTime.UtcNow.AddDays(-RetentionDays);

            _logger.LogInformation(
                "Cleaning audit trail records older than {CutoffDate} ({RetentionDays} days)",
                cutoffDate,
                RetentionDays);

            // Get count of records to be deleted
            var recordsToDeleteCount = await _context.AuditTrails
                .Where(a => a.Timestamp < cutoffDate)
                .CountAsync(cancellationToken);

            if (recordsToDeleteCount == 0)
            {
                _logger.LogInformation("No old audit trail records found to clean");
                return;
            }

            _logger.LogInformation(
                "Found {Count} audit trail records to delete",
                recordsToDeleteCount);

            // Delete in batches to avoid locking the table for too long
            const int batchSize = 1000;
            var totalDeleted = 0;

            while (true)
            {
                // Get a batch of old audit trail records
                var recordsToDelete = await _context.AuditTrails
                    .Where(a => a.Timestamp < cutoffDate)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                if (!recordsToDelete.Any())
                {
                    break;
                }

                // Remove the batch
                _context.AuditTrails.RemoveRange(recordsToDelete);
                await _context.SaveChangesAsync(cancellationToken);

                totalDeleted += recordsToDelete.Count;

                _logger.LogInformation(
                    "Deleted batch of {BatchCount} audit trail records. Total deleted: {TotalDeleted}",
                    recordsToDelete.Count,
                    totalDeleted);

                // Small delay to prevent overwhelming the database
                if (recordsToDelete.Count == batchSize)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }

            _logger.LogInformation(
                "CleanOldAuditTrailJob completed. Total records deleted: {TotalDeleted}",
                totalDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in CleanOldAuditTrailJob");
            throw;
        }
    }

    /// <summary>
    /// Clean audit trails with custom retention period
    /// </summary>
    public async Task ExecuteAsync(int retentionDays, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting CleanOldAuditTrailJob with custom retention period: {RetentionDays} days",
            retentionDays);

        try
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

            var recordsToDeleteCount = await _context.AuditTrails
                .Where(a => a.Timestamp < cutoffDate)
                .CountAsync(cancellationToken);

            if (recordsToDeleteCount == 0)
            {
                _logger.LogInformation("No old audit trail records found to clean");
                return;
            }

            _logger.LogInformation(
                "Found {Count} audit trail records older than {CutoffDate}",
                recordsToDeleteCount,
                cutoffDate);

            const int batchSize = 1000;
            var totalDeleted = 0;

            while (true)
            {
                var recordsToDelete = await _context.AuditTrails
                    .Where(a => a.Timestamp < cutoffDate)
                    .Take(batchSize)
                    .ToListAsync(cancellationToken);

                if (!recordsToDelete.Any())
                {
                    break;
                }

                _context.AuditTrails.RemoveRange(recordsToDelete);
                await _context.SaveChangesAsync(cancellationToken);

                totalDeleted += recordsToDelete.Count;

                _logger.LogInformation(
                    "Deleted batch of {BatchCount} records. Total: {TotalDeleted}/{TotalToDelete}",
                    recordsToDelete.Count,
                    totalDeleted,
                    recordsToDeleteCount);

                if (recordsToDelete.Count == batchSize)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }

            _logger.LogInformation(
                "CleanOldAuditTrailJob completed with custom retention. Total deleted: {TotalDeleted}",
                totalDeleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in CleanOldAuditTrailJob with custom retention");
            throw;
        }
    }
}
