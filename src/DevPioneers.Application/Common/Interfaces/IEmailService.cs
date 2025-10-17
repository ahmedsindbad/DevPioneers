// ============================================
// File: DevPioneers.Application/Common/Interfaces/IEmailService.cs
// Email Service Interface
// ============================================
namespace DevPioneers.Application.Common.Interfaces;

/// <summary>
/// Email service interface for sending various types of emails
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send welcome email with verification link
    /// </summary>
    Task<bool> SendWelcomeEmailAsync(string toEmail, string fullName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send OTP code via email
    /// </summary>
    Task<bool> SendOtpEmailAsync(string toEmail, string otpCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send password reset email with reset link
    /// </summary>
    Task<bool> SendPasswordResetEmailAsync(string toEmail, string resetToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send subscription confirmation email
    /// </summary>
    Task<bool> SendSubscriptionConfirmationAsync(string toEmail, string planName, DateTime expiryDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send payment receipt email
    /// </summary>
    Task<bool> SendPaymentReceiptAsync(string toEmail, decimal amount, string currency, string referenceNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send subscription expiry reminder
    /// </summary>
    Task<bool> SendSubscriptionExpiryReminderAsync(string toEmail, string planName, int daysRemaining, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send wallet transaction notification
    /// </summary>
    Task<bool> SendWalletTransactionNotificationAsync(string toEmail, string transactionType, decimal amount, decimal newBalance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send custom email with subject and body
    /// </summary>
    Task<bool> SendEmailAsync(string toEmail, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Send bulk emails to multiple recipients
    /// </summary>
    Task<int> SendBulkEmailsAsync(List<string> toEmails, string subject, string body, bool isHtml = false, CancellationToken cancellationToken = default);
}