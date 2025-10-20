// ============================================
// File: DevPioneers.Infrastructure/Services/BackgroundJobs/SendEmailJob.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Infrastructure.Services.BackgroundJobs;

/// <summary>
/// Background job to send queued emails
/// This can be used to send emails asynchronously via Hangfire
/// </summary>
public class SendEmailJob : ISendEmailJob
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendEmailJob> _logger;

    public SendEmailJob(
        IEmailService emailService,
        ILogger<SendEmailJob> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    /// <summary>
    /// Execute the send email job
    /// This is a general-purpose email sender that can be called from Hangfire
    /// </summary>
    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SendEmailJob Execute called at {Time}", DateTime.UtcNow);

        // This is a base implementation. In a real scenario, you might have an EmailQueue table
        // that stores emails to be sent. For now, this is a placeholder that can be
        // called directly via Hangfire with specific parameters.

        _logger.LogInformation("SendEmailJob completed successfully");
    }

    /// <summary>
    /// Send a specific email (can be called directly from Hangfire)
    /// </summary>
    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending email to {Email} with subject: {Subject}",
            toEmail,
            subject);

        try
        {
            var result = await _emailService.SendEmailAsync(
                toEmail,
                subject,
                body,
                isHtml,
                cancellationToken);

            if (result)
            {
                _logger.LogInformation("Email sent successfully to {Email}", toEmail);
            }
            else
            {
                _logger.LogWarning("Failed to send email to {Email}", toEmail);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email}", toEmail);
            throw;
        }
    }

    /// <summary>
    /// Send subscription expiry reminders
    /// This job sends reminders to users whose subscriptions are expiring soon
    /// </summary>
    public async Task SendSubscriptionExpiryRemindersAsync(
        IApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting subscription expiry reminder emails at {Time}", DateTime.UtcNow);

        try
        {
            // Find subscriptions expiring in the next 7 days
            var expiryDate = DateTime.UtcNow.AddDays(7);
            var today = DateTime.UtcNow;

            var expiringSubscriptions = await context.UserSubscriptions
                .Include(s => s.User)
                .Include(s => s.SubscriptionPlan)
                .Where(s => s.Status == Domain.Enums.SubscriptionStatus.Active &&
                           s.EndDate > today &&
                           s.EndDate <= expiryDate)
                .ToListAsync(cancellationToken);

            if (!expiringSubscriptions.Any())
            {
                _logger.LogInformation("No expiring subscriptions found");
                return;
            }

            _logger.LogInformation("Found {Count} expiring subscriptions", expiringSubscriptions.Count);

            var sentCount = 0;
            var errorCount = 0;

            foreach (var subscription in expiringSubscriptions)
            {
                try
                {
                    var daysRemaining = (subscription.EndDate - DateTime.UtcNow).Days;

                    await _emailService.SendSubscriptionExpiryReminderAsync(
                        subscription.User.Email,
                        subscription.SubscriptionPlan.Name,
                        daysRemaining,
                        cancellationToken);

                    sentCount++;
                    _logger.LogInformation(
                        "Sent expiry reminder to user {UserId} for subscription {SubscriptionId}",
                        subscription.UserId,
                        subscription.Id);
                }
                catch (Exception ex)
                {
                    errorCount++;
                    _logger.LogError(
                        ex,
                        "Error sending expiry reminder to user {UserId}",
                        subscription.UserId);
                }
            }

            _logger.LogInformation(
                "Subscription expiry reminders completed. Sent: {SentCount}, Errors: {ErrorCount}",
                sentCount,
                errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error in SendSubscriptionExpiryRemindersAsync");
            throw;
        }
    }

    /// <summary>
    /// Send bulk emails to a list of recipients
    /// </summary>
    public async Task SendBulkEmailsAsync(
        List<string> recipients,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Starting bulk email send to {Count} recipients",
            recipients.Count);

        try
        {
            var sentCount = await _emailService.SendBulkEmailsAsync(
                recipients,
                subject,
                body,
                isHtml,
                cancellationToken);

            _logger.LogInformation(
                "Bulk email send completed. Sent: {SentCount} out of {TotalCount}",
                sentCount,
                recipients.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in bulk email send");
            throw;
        }
    }
}
