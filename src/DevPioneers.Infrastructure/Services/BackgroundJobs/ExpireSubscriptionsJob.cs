// ============================================
// File: DevPioneers.Infrastructure/Services/BackgroundJobs/ExpireSubscriptionsJob.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Background job to expire subscriptions that have passed their end date
/// </summary>
public class ExpireSubscriptionsJob : IExpireSubscriptionsJob
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<ExpireSubscriptionsJob> _logger;

    public ExpireSubscriptionsJob(
        IApplicationDbContext context,
        IEmailService emailService,
        ILogger<ExpireSubscriptionsJob> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ExpireSubscriptionsJob at {Time}", DateTime.UtcNow);

        try
        {
            // Find subscriptions that should be expired
            var now = DateTime.UtcNow;
            var subscriptionsToExpire = await _context.UserSubscriptions
                .Include(s => s.User)
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.EndDate < now &&
                           (s.Status == SubscriptionStatus.Active ||
                            s.Status == SubscriptionStatus.Trial ||
                            s.Status == SubscriptionStatus.GracePeriod))
                .ToListAsync(cancellationToken);

            if (!subscriptionsToExpire.Any())
            {
                _logger.LogInformation("No subscriptions found to expire");
                return;
            }

            _logger.LogInformation("Found {Count} subscriptions to expire", subscriptionsToExpire.Count);

            var expiredCount = 0;
            var errorCount = 0;

            foreach (var subscription in subscriptionsToExpire)
            {
                try
                {
                    // Update subscription status to expired
                    subscription.Status = SubscriptionStatus.Expired;
                    subscription.AutoRenewal = false;

                    // Send expiration notification email
                    await _emailService.SendEmailAsync(
                        subscription.User.Email,
                        "Subscription Expired",
                        $"Dear {subscription.User.FullName},<br/><br/>" +
                        $"Your subscription to '{subscription.SubscriptionPlan.Name}' has expired on {subscription.EndDate:dd/MM/yyyy}.<br/>" +
                        $"Please renew your subscription to continue enjoying our services.<br/><br/>" +
                        $"Thank you,<br/>DevPioneers Team",
                        isHtml: true,
                        cancellationToken);

                    expiredCount++;
                    _logger.LogInformation(
                        "Expired subscription {SubscriptionId} for user {UserId}",
                        subscription.Id,
                        subscription.UserId);
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(
                        ex,
                        "Error expiring subscription {SubscriptionId} for user {UserId}",
                        subscription.Id,
                        subscription.UserId);
                }
            }

            // Save all changes at once
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "ExpireSubscriptionsJob completed. Expired: {ExpiredCount}, Errors: {ErrorCount}",
                expiredCount,
                errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in ExpireSubscriptionsJob");
            throw;
        }
    }
}
