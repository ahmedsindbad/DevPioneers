// ============================================
// File: DevPioneers.Infrastructure/Configurations/EmailSettings.cs
// ============================================
namespace DevPioneers.Infrastructure.Configurations;

/// <summary>
/// Email configuration settings
/// </summary>
public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    /// <summary>
    /// SMTP server address
    /// </summary>
    public string SmtpServer { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port
    /// </summary>
    public int SmtpPort { get; set; } = 587;

    /// <summary>
    /// Sender name
    /// </summary>
    public string SenderName { get; set; } = string.Empty;

    /// <summary>
    /// Sender email address
    /// </summary>
    public string SenderEmail { get; set; } = string.Empty;

    /// <summary>
    /// SMTP username
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// SMTP password
    /// </summary>
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Enable SSL/TLS
    /// </summary>
    public bool EnableSsl { get; set; } = true;

    /// <summary>
    /// Enable email sending
    /// </summary>
    public bool EnableEmailSending { get; set; } = true;

    /// <summary>
    /// Validate settings
    /// </summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(SmtpServer))
            throw new InvalidOperationException("SMTP server is required");

        if (SmtpPort <= 0 || SmtpPort > 65535)
            throw new InvalidOperationException("SMTP port must be between 1 and 65535");

        if (string.IsNullOrWhiteSpace(SenderEmail))
            throw new InvalidOperationException("Sender email is required");

        if (string.IsNullOrWhiteSpace(Username))
            throw new InvalidOperationException("SMTP username is required");

        if (string.IsNullOrWhiteSpace(Password))
            throw new InvalidOperationException("SMTP password is required");
    }
}
