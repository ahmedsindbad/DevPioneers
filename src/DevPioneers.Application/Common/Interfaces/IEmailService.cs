// ============================================
// File: DevPioneers.Application/Common/Interfaces/IEmailService.cs
// ============================================
namespace DevPioneers.Application.Common.Interfaces;

/// <summary>
/// Email service interface
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Send email to single recipient
    /// </summary>
    Task SendEmailAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send email to multiple recipients
    /// </summary>
    Task SendEmailAsync(
        IEnumerable<string> to,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send email with template
    /// </summary>
    Task SendTemplateEmailAsync<T>(
        string to,
        string templateName,
        T model,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Send OTP email
    /// </summary>
    Task SendOtpEmailAsync(
        string to,
        string otpCode,
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send welcome email
    /// </summary>
    Task SendWelcomeEmailAsync(
        string to,
        string userName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send password reset email
    /// </summary>
    Task SendPasswordResetEmailAsync(
        string to,
        string userName,
        string resetLink,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send subscription confirmation email
    /// </summary>
    Task SendSubscriptionConfirmationEmailAsync(
        string to,
        string userName,
        string planName,
        decimal amount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Send payment receipt email
    /// </summary>
    Task SendPaymentReceiptEmailAsync(
        string to,
        string userName,
        string receiptDetails,
        CancellationToken cancellationToken = default);
}