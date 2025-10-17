// ============================================
// File: DevPioneers.Infrastructure/Services/MockEmailService.cs
// Mock Email Service for Development/Testing
// ============================================
using DevPioneers.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;

namespace DevPioneers.Infrastructure.Services;

/// <summary>
/// Mock implementation of Email Service for development
/// In production, replace with real email service (SendGrid, SMTP, etc.)
/// </summary>
public class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;
    private readonly IDateTime _dateTime;

    public MockEmailService(ILogger<MockEmailService> logger, IDateTime dateTime)
    {
        _logger = logger;
        _dateTime = dateTime;
    }

    /// <summary>
    /// Send welcome email with verification link
    /// </summary>
    public async Task<bool> SendWelcomeEmailAsync(string toEmail, string fullName, CancellationToken cancellationToken = default)
    {
        try
        {
            var emailBody = new StringBuilder();
            emailBody.AppendLine($"Dear {fullName},");
            emailBody.AppendLine();
            emailBody.AppendLine("Welcome to DevPioneers! 🎉");
            emailBody.AppendLine();
            emailBody.AppendLine("Thank you for registering with us. We're excited to have you on board!");
            emailBody.AppendLine();
            emailBody.AppendLine("To get started, please verify your email address by clicking the link below:");
            emailBody.AppendLine($"[Verification Link: http://localhost:5000/verify-email?token=mock-token-{Guid.NewGuid()}]");
            emailBody.AppendLine();
            emailBody.AppendLine("This link will expire in 24 hours.");
            emailBody.AppendLine();
            emailBody.AppendLine("Best regards,");
            emailBody.AppendLine("DevPioneers Team");

            LogEmail("Welcome Email", toEmail, "Welcome to DevPioneers! Please verify your email", emailBody.ToString());
            
            // Simulate async operation
            await Task.Delay(100, cancellationToken);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to {Email}", toEmail);
            return false;
        }
    }

    /// <summary>
    /// Send OTP code via email
    /// </summary>
    public async Task<bool> SendOtpEmailAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default)
    {
        try
        {
            var emailBody = new StringBuilder();
            emailBody.AppendLine("Your One-Time Password (OTP)");
            emailBody.AppendLine();
            emailBody.AppendLine($"Your verification code is: {otpCode}");
            emailBody.AppendLine();
            emailBody.AppendLine("This code is valid for 10 minutes.");
            emailBody.AppendLine();
            emailBody.AppendLine("If you didn't request this code, please ignore this email.");
            emailBody.AppendLine();
            emailBody.AppendLine("Best regards,");
            emailBody.AppendLine("DevPioneers Security Team");

            LogEmail("OTP Email", toEmail, $"Your verification code: {otpCode}", emailBody.ToString());
            
            // Simulate async operation
            await Task.Delay(100, cancellationToken);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send OTP email to {Email}", toEmail);
            return false;
        }
    }

    /// <summary>
    /// Send password reset email
    /// </summary>
    public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var emailBody = new StringBuilder();
            emailBody.AppendLine("Password Reset Request");
            emailBody.AppendLine();
            emailBody.AppendLine("We received a request to reset your password.");
            emailBody.AppendLine();
            emailBody.AppendLine("Click the link below to reset your password:");
            emailBody.AppendLine($"[Reset Link: http://localhost:5000/reset-password?token={resetToken}]");
            emailBody.AppendLine();
            emailBody.AppendLine("This link will expire in 1 hour.");
            emailBody.AppendLine();
            emailBody.AppendLine("If you didn't request this, please ignore this email.");
            emailBody.AppendLine();
            emailBody.AppendLine("Best regards,");
            emailBody.AppendLine("DevPioneers Security Team");

            LogEmail("Password Reset Email", toEmail, "Reset Your Password", emailBody.ToString());
            
            // Simulate async operation
            await Task.Delay(100, cancellationToken);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", toEmail);
            return false;
        }
    }

    /// <summary>
    /// Send subscription confirmation email
    /// </summary>
    public async Task<bool> SendSubscriptionConfirmationAsync(string toEmail, string planName, DateTime expiryDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var emailBody = new StringBuilder();
            emailBody.AppendLine("Subscription Confirmed! 🎉");
            emailBody.AppendLine();
            emailBody.AppendLine($"Your subscription to {planName} has been activated successfully.");
            emailBody.AppendLine();
            emailBody.AppendLine("Subscription Details:");
            emailBody.AppendLine($"- Plan: {planName}");
            emailBody.AppendLine($"- Valid Until: {expiryDate:MMMM dd, yyyy}");
            emailBody.AppendLine();
            emailBody.AppendLine("You now have access to all premium features.");
            emailBody.AppendLine();
            emailBody.AppendLine("Thank you for choosing DevPioneers!");
            emailBody.AppendLine();
            emailBody.AppendLine("Best regards,");
            emailBody.AppendLine("DevPioneers Team");

            LogEmail("Subscription Confirmation", toEmail, $"Subscription to {planName} Activated", emailBody.ToString());
            
            // Simulate async operation
            await Task.Delay(100, cancellationToken);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send subscription confirmation to {Email}", toEmail);
            return false;
        }
    }

    /// <summary>
    /// Send payment receipt email
    /// </summary>
    public async Task<bool> SendPaymentReceiptAsync(string toEmail, decimal amount, string currency, string referenceNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            var emailBody = new StringBuilder();
            emailBody.AppendLine("Payment Receipt");
            emailBody.AppendLine();
            emailBody.AppendLine($"Thank you for your payment of {currency} {amount:F2}");
            emailBody.AppendLine();
            emailBody.AppendLine("Payment Details:");
            emailBody.AppendLine($"- Reference Number: {referenceNumber}");
            emailBody.AppendLine($"- Amount: {currency} {amount:F2}");
            emailBody.AppendLine($"- Date: {_dateTime.UtcNow:MMMM dd, yyyy HH:mm} UTC");
            emailBody.AppendLine($"- Status: Successful");
            emailBody.AppendLine();
            emailBody.AppendLine("This receipt serves as confirmation of your payment.");
            emailBody.AppendLine();
            emailBody.AppendLine("Best regards,");
            emailBody.AppendLine("DevPioneers Billing Team");

            LogEmail("Payment Receipt", toEmail, $"Payment Receipt - {referenceNumber}", emailBody.ToString());
            
            // Simulate async operation
            await Task.Delay(100, cancellationToken);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send payment receipt to {Email}", toEmail);
            return false;
        }
    }

    /// <summary>
    /// Send subscription expiry reminder
    /// </summary>
    public async Task<bool> SendSubscriptionExpiryReminderAsync(string toEmail, string planName, int daysRemaining, CancellationToken cancellationToken = default)
    {
        try
        {
            var emailBody = new StringBuilder();
            emailBody.AppendLine("Subscription Expiring Soon");
            emailBody.AppendLine();
            emailBody.AppendLine($"Your {planName} subscription will expire in {daysRemaining} day(s).");
            emailBody.AppendLine();
            emailBody.AppendLine("To continue enjoying uninterrupted service, please renew your subscription.");
            emailBody.AppendLine();
            emailBody.AppendLine("[Renew Now: http://localhost:5000/subscription/renew]");
            emailBody.AppendLine();
            emailBody.AppendLine("If you have any questions, please don't hesitate to contact us.");
            emailBody.AppendLine();
            emailBody.AppendLine("Best regards,");
            emailBody.AppendLine("DevPioneers Team");

            LogEmail("Subscription Reminder", toEmail, $"Your {planName} subscription expires in {daysRemaining} days", emailBody.ToString());
            
            // Simulate async operation
            await Task.Delay(100, cancellationToken);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send subscription reminder to {Email}", toEmail);
            return false;
        }
    }

    /// <summary>
    /// Send wallet transaction notification
    /// </summary>
    public async Task<bool> SendWalletTransactionNotificationAsync(string toEmail, string transactionType, decimal amount, decimal newBalance, CancellationToken cancellationToken = default)
    {
        try
        {
            var emailBody = new StringBuilder();
            emailBody.AppendLine($"Wallet {transactionType} Notification");
            emailBody.AppendLine();
            emailBody.AppendLine($"A {transactionType.ToLower()} of EGP {amount:F2} has been processed.");
            emailBody.AppendLine();
            emailBody.AppendLine("Transaction Details:");
            emailBody.AppendLine($"- Type: {transactionType}");
            emailBody.AppendLine($"- Amount: EGP {amount:F2}");
            emailBody.AppendLine($"- New Balance: EGP {newBalance:F2}");
            emailBody.AppendLine($"- Date: {_dateTime.UtcNow:MMMM dd, yyyy HH:mm} UTC");
            emailBody.AppendLine();
            emailBody.AppendLine("If you didn't authorize this transaction, please contact us immediately.");
            emailBody.AppendLine();
            emailBody.AppendLine("Best regards,");
            emailBody.AppendLine("DevPioneers Team");

            LogEmail("Wallet Transaction", toEmail, $"Wallet {transactionType} - EGP {amount:F2}", emailBody.ToString());
            
            // Simulate async operation
            await Task.Delay(100, cancellationToken);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send wallet transaction notification to {Email}", toEmail);
            return false;
        }
    }

    /// <summary>
    /// Send custom email
    /// </summary>
    public async Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default)
    {
        try
        {
            LogEmail("Custom Email", toEmail, subject, body, isHtml);
            
            // Simulate async operation
            await Task.Delay(100, cancellationToken);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send custom email to {Email}", toEmail);
            return false;
        }
    }

    /// <summary>
    /// Send bulk emails
    /// </summary>
    public async Task<int> SendBulkEmailsAsync(List<string> toEmails, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default)
    {
        var successCount = 0;
        
        foreach (var email in toEmails)
        {
            if (await SendEmailAsync(email, subject, body, isHtml, cancellationToken))
            {
                successCount++;
            }
            
            // Add delay to simulate rate limiting
            await Task.Delay(50, cancellationToken);
        }
        
        _logger.LogInformation("Bulk email sent to {SuccessCount}/{TotalCount} recipients", successCount, toEmails.Count);
        
        return successCount;
    }

    /// <summary>
    /// Log email details (for development/debugging)
    /// </summary>
    private void LogEmail(string emailType, string toEmail, string subject, string body, bool isHtml = false)
    {
        _logger.LogInformation(
            "📧 MOCK EMAIL SENT:\n" +
            "=====================================\n" +
            "Type: {EmailType}\n" +
            "To: {ToEmail}\n" +
            "Subject: {Subject}\n" +
            "IsHtml: {IsHtml}\n" +
            "Timestamp: {Timestamp}\n" +
            "Body:\n{Body}\n" +
            "=====================================",
            emailType,
            toEmail,
            subject,
            isHtml,
            _dateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
            body
        );
    }
}