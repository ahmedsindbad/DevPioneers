// ============================================
// File: DevPioneers.Infrastructure/Services/SmtpEmailService.cs
// ============================================
using System.Net;
using System.Net.Mail;
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Infrastructure.Configurations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevPioneers.Infrastructure.Services;

/// <summary>
/// SMTP email service implementation using System.Net.Mail
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly EmailSettings _settings;

    public SmtpEmailService(
        ILogger<SmtpEmailService> logger,
        IOptions<EmailSettings> settings)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

        if (_settings.EnableEmailSending)
        {
            _settings.Validate();
        }
    }

    /// <inheritdoc />
    public async Task SendWelcomeEmailAsync(
        string toEmail,
        string userName,
        CancellationToken cancellationToken = default)
    {
        var subject = "Welcome to DevPioneers!";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Welcome to DevPioneers, {userName}!</h2>
                <p>Thank you for joining our community of innovative developers.</p>
                <p>We're excited to have you on board and look forward to helping you achieve your goals.</p>
                <p>Get started by exploring our features and setting up your profile.</p>
                <br/>
                <p>Best regards,<br/>The DevPioneers Team</p>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendOtpEmailAsync(
        string toEmail,
        string otpCode,
        int expirationMinutes = 5,
        CancellationToken cancellationToken = default)
    {
        var subject = $"DevPioneers - Verification Code: {otpCode}";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Verification Code</h2>
                <p>Your DevPioneers verification code is:</p>
                <h1 style='color: #007bff; letter-spacing: 5px;'>{otpCode}</h1>
                <p>This code will expire in {expirationMinutes} minutes.</p>
                <p>If you didn't request this code, please ignore this email.</p>
                <br/>
                <p>Best regards,<br/>The DevPioneers Team</p>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendPasswordResetEmailAsync(
        string toEmail,
        string resetToken,
        string resetUrl,
        CancellationToken cancellationToken = default)
    {
        var subject = "DevPioneers - Password Reset Request";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Password Reset Request</h2>
                <p>We received a request to reset your password.</p>
                <p>Click the button below to reset your password:</p>
                <a href='{resetUrl}' style='display: inline-block; padding: 12px 24px; background-color: #007bff; color: white; text-decoration: none; border-radius: 4px; margin: 20px 0;'>
                    Reset Password
                </a>
                <p>Or copy and paste this link into your browser:</p>
                <p style='color: #666; word-break: break-all;'>{resetUrl}</p>
                <p>This link will expire in 1 hour.</p>
                <p>If you didn't request a password reset, please ignore this email.</p>
                <br/>
                <p>Best regards,<br/>The DevPioneers Team</p>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendSubscriptionConfirmationAsync(
        string toEmail,
        string userName,
        string planName,
        decimal amount,
        DateTime expiryDate,
        CancellationToken cancellationToken = default)
    {
        var subject = "DevPioneers - Subscription Confirmed";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Subscription Confirmed</h2>
                <p>Hi {userName},</p>
                <p>Your subscription to <strong>{planName}</strong> has been confirmed!</p>
                <div style='background-color: #f8f9fa; padding: 15px; border-radius: 4px; margin: 20px 0;'>
                    <p><strong>Plan:</strong> {planName}</p>
                    <p><strong>Amount:</strong> ${amount:F2}</p>
                    <p><strong>Expires:</strong> {expiryDate:MMMM dd, yyyy}</p>
                </div>
                <p>Thank you for choosing DevPioneers!</p>
                <br/>
                <p>Best regards,<br/>The DevPioneers Team</p>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendPaymentReceiptAsync(
        string toEmail,
        string userName,
        string transactionId,
        decimal amount,
        DateTime paymentDate,
        CancellationToken cancellationToken = default)
    {
        var subject = $"DevPioneers - Payment Receipt #{transactionId}";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Payment Receipt</h2>
                <p>Hi {userName},</p>
                <p>Thank you for your payment. Here are the details:</p>
                <div style='background-color: #f8f9fa; padding: 15px; border-radius: 4px; margin: 20px 0;'>
                    <p><strong>Transaction ID:</strong> {transactionId}</p>
                    <p><strong>Amount:</strong> ${amount:F2}</p>
                    <p><strong>Date:</strong> {paymentDate:MMMM dd, yyyy HH:mm}</p>
                </div>
                <p>Keep this email for your records.</p>
                <br/>
                <p>Best regards,<br/>The DevPioneers Team</p>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendSubscriptionExpiryReminderAsync(
        string toEmail,
        string userName,
        string planName,
        DateTime expiryDate,
        int daysRemaining,
        CancellationToken cancellationToken = default)
    {
        var subject = "DevPioneers - Subscription Expiring Soon";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Subscription Expiring Soon</h2>
                <p>Hi {userName},</p>
                <p>Your <strong>{planName}</strong> subscription will expire in <strong>{daysRemaining} days</strong>.</p>
                <div style='background-color: #fff3cd; padding: 15px; border-radius: 4px; margin: 20px 0; border-left: 4px solid #ffc107;'>
                    <p><strong>Expiry Date:</strong> {expiryDate:MMMM dd, yyyy}</p>
                </div>
                <p>Renew now to continue enjoying all the features of DevPioneers.</p>
                <a href='https://devpioneers.com/subscriptions' style='display: inline-block; padding: 12px 24px; background-color: #007bff; color: white; text-decoration: none; border-radius: 4px; margin: 20px 0;'>
                    Renew Subscription
                </a>
                <br/>
                <p>Best regards,<br/>The DevPioneers Team</p>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendWalletTransactionNotificationAsync(
        string toEmail,
        string userName,
        string transactionType,
        decimal amount,
        decimal newBalance,
        CancellationToken cancellationToken = default)
    {
        var subject = $"DevPioneers - Wallet {transactionType}";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Wallet Transaction</h2>
                <p>Hi {userName},</p>
                <p>A transaction has been processed on your wallet:</p>
                <div style='background-color: #f8f9fa; padding: 15px; border-radius: 4px; margin: 20px 0;'>
                    <p><strong>Type:</strong> {transactionType}</p>
                    <p><strong>Amount:</strong> ${amount:F2}</p>
                    <p><strong>New Balance:</strong> ${newBalance:F2}</p>
                </div>
                <p>If you have any questions, please contact our support team.</p>
                <br/>
                <p>Best regards,<br/>The DevPioneers Team</p>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendEmailAsync(
        string toEmail,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(toEmail))
            throw new ArgumentNullException(nameof(toEmail));

        if (string.IsNullOrWhiteSpace(subject))
            throw new ArgumentNullException(nameof(subject));

        if (string.IsNullOrWhiteSpace(body))
            throw new ArgumentNullException(nameof(body));

        if (!_settings.EnableEmailSending)
        {
            _logger.LogWarning(
                "Email sending is disabled. Email to {ToEmail} with subject '{Subject}' was not sent",
                toEmail, subject);
            return;
        }

        try
        {
            using var smtpClient = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
            {
                EnableSsl = _settings.EnableSsl,
                Credentials = new NetworkCredential(_settings.Username, _settings.Password),
                Timeout = 30000 // 30 seconds
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);

            await smtpClient.SendMailAsync(mailMessage, cancellationToken);

            _logger.LogInformation(
                "Email sent successfully to {ToEmail} with subject '{Subject}'",
                toEmail, subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send email to {ToEmail} with subject '{Subject}'",
                toEmail, subject);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task SendBulkEmailsAsync(
        List<string> toEmails,
        string subject,
        string body,
        CancellationToken cancellationToken = default)
    {
        if (toEmails == null || !toEmails.Any())
            throw new ArgumentNullException(nameof(toEmails));

        var tasks = toEmails.Select(email =>
            SendEmailAsync(email, subject, body, cancellationToken));

        await Task.WhenAll(tasks);

        _logger.LogInformation(
            "Bulk email sent to {Count} recipients with subject '{Subject}'",
            toEmails.Count, subject);
    }

    /// <inheritdoc />
    public async Task SendEmailVerificationAsync(
        string toEmail,
        string verificationCode,
        CancellationToken cancellationToken = default)
    {
        var subject = $"DevPioneers - Email Verification Code: {verificationCode}";
        var body = $@"
            <html>
            <body style='font-family: Arial, sans-serif;'>
                <h2>Email Verification</h2>
                <p>Please verify your email address using the code below:</p>
                <h1 style='color: #007bff; letter-spacing: 5px;'>{verificationCode}</h1>
                <p>This code will expire in 5 minutes.</p>
                <p>If you didn't request this verification, please ignore this email.</p>
                <br/>
                <p>Best regards,<br/>The DevPioneers Team</p>
            </body>
            </html>";

        await SendEmailAsync(toEmail, subject, body, cancellationToken);
    }

    /// <inheritdoc />
    public async Task SendMobileVerificationOtpAsync(
        string mobileNumber,
        string otpCode,
        CancellationToken cancellationToken = default)
    {
        // Note: This is a placeholder for SMS sending
        // In production, you would integrate with an SMS gateway like Twilio, AWS SNS, etc.
        _logger.LogInformation(
            "SMS OTP would be sent to {MobileNumber}: {OtpCode}",
            MaskMobile(mobileNumber), otpCode);

        // For now, we'll just log it
        // In production, implement actual SMS sending logic here

        await Task.CompletedTask;
    }

    #region Private Methods

    /// <summary>
    /// Mask mobile number for logging (show first 2 and last 2 digits)
    /// </summary>
    private static string MaskMobile(string mobile)
    {
        if (mobile.Length <= 4)
            return "****";

        return $"{mobile[..2]}****{mobile[^2..]}";
    }

    #endregion
}
