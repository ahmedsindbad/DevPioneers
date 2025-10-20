// ============================================
// File: DevPioneers.Infrastructure/Configurations/OtpSettings.cs
// ============================================
namespace DevPioneers.Infrastructure.Configurations;

/// <summary>
/// OTP configuration settings
/// </summary>
public class OtpSettings
{
    public const string SectionName = "OtpSettings";

    /// <summary>
    /// OTP expiration time in minutes
    /// </summary>
    public int ExpirationMinutes { get; set; } = 5;

    /// <summary>
    /// OTP code length
    /// </summary>
    public int CodeLength { get; set; } = 6;

    /// <summary>
    /// Maximum verification attempts
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Delay before allowing resend in minutes
    /// </summary>
    public int ResendDelayMinutes { get; set; } = 1;

    /// <summary>
    /// Enable SMS OTP
    /// </summary>
    public bool EnableSmsOtp { get; set; } = true;

    /// <summary>
    /// Enable Email OTP
    /// </summary>
    public bool EnableEmailOtp { get; set; } = true;

    /// <summary>
    /// SMS provider (Mock, Twilio, etc.)
    /// </summary>
    public string SmsProvider { get; set; } = "Mock";

    /// <summary>
    /// Email subject template
    /// </summary>
    public string EmailSubjectTemplate { get; set; } = "DevPioneers - Verification Code: {0}";

    /// <summary>
    /// SMS message template
    /// </summary>
    public string SmsMessageTemplate { get; set; } = "Your DevPioneers verification code is: {0}. Valid for {1} minutes.";

    /// <summary>
    /// Validate settings
    /// </summary>
    public void Validate()
    {
        if (ExpirationMinutes <= 0)
            throw new InvalidOperationException("OTP expiration minutes must be positive");

        if (CodeLength < 4 || CodeLength > 8)
            throw new InvalidOperationException("OTP code length must be between 4 and 8");

        if (MaxAttempts <= 0)
            throw new InvalidOperationException("Max attempts must be positive");

        if (ResendDelayMinutes < 0)
            throw new InvalidOperationException("Resend delay cannot be negative");
    }
}
