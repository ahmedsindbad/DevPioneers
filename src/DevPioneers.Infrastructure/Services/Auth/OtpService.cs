// ============================================
// File: DevPioneers.Infrastructure/Services/Auth/OtpService.cs
// ============================================
using System.Security.Cryptography;
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Domain.Entities;
using DevPioneers.Infrastructure.Configurations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DevPioneers.Infrastructure.Services.Auth;

/// <summary>
/// OTP service implementation
/// </summary>
public class OtpService : IOtpService
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly ILogger<OtpService> _logger;
    private readonly OtpSettings _settings;

    public OtpService(
        IApplicationDbContext context,
        IEmailService emailService,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        ILogger<OtpService> logger,
        IOptions<OtpSettings> settings)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _currentUserService = currentUserService ?? throw new ArgumentNullException(nameof(currentUserService));
        _dateTime = dateTime ?? throw new ArgumentNullException(nameof(dateTime));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

        _settings.Validate();
    }

    /// <inheritdoc />
    public async Task<string> GenerateOtpAsync(
        string identifier,
        string purpose,
        int? userId = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentNullException(nameof(identifier));

        if (string.IsNullOrWhiteSpace(purpose))
            throw new ArgumentNullException(nameof(purpose));

        try
        {
            // Invalidate previous OTPs for this identifier and purpose
            await InvalidateOtpAsync(identifier, purpose, cancellationToken);

            // Generate OTP code
            var code = GenerateRandomCode(_settings.CodeLength);
            var hashedCode = HashCode(code);

            // Determine if identifier is email or mobile
            var isEmail = identifier.Contains('@');

            // Create OTP entity
            var otpCode = new OtpCode
            {
                UserId = userId,
                Mobile = isEmail ? string.Empty : identifier,
                Email = isEmail ? identifier : null,
                Code = hashedCode,
                Purpose = purpose,
                ExpiresAt = _dateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes),
                MaxAttempts = _settings.MaxAttempts,
                IpAddress = _currentUserService.IpAddress,
                CreatedAtUtc = _dateTime.UtcNow
            };

            _context.OtpCodes.Add(otpCode);
            await _context.SaveChangesAsync(cancellationToken);

            // Send OTP via appropriate channel
            if (isEmail && _settings.EnableEmailOtp)
            {
                await _emailService.SendOtpEmailAsync(
                    identifier,
                    code,
                    cancellationToken);

                _logger.LogInformation(
                    "OTP sent via email to {Identifier} for purpose {Purpose}",
                    MaskIdentifier(identifier), purpose);
            }
            else if (!isEmail && _settings.EnableSmsOtp)
            {
                await _emailService.SendMobileVerificationOtpAsync(
                    identifier,
                    code,
                    cancellationToken);

                _logger.LogInformation(
                    "OTP sent via SMS to {Identifier} for purpose {Purpose}",
                    MaskIdentifier(identifier), purpose);
            }

            _logger.LogInformation(
                "OTP generated for {Identifier} with purpose {Purpose}. Expires at {ExpiresAt}",
                MaskIdentifier(identifier), purpose, otpCode.ExpiresAt);

            return code;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error generating OTP for {Identifier} with purpose {Purpose}",
                MaskIdentifier(identifier), purpose);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> VerifyOtpAsync(
        string identifier,
        string code,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentNullException(nameof(identifier));

        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentNullException(nameof(code));

        if (string.IsNullOrWhiteSpace(purpose))
            throw new ArgumentNullException(nameof(purpose));

        try
        {
            var isEmail = identifier.Contains('@');
            var hashedCode = HashCode(code);

            // Find the most recent valid OTP
            var otpCode = await _context.OtpCodes
                .Where(o => o.Purpose == purpose)
                .Where(o => isEmail
                    ? o.Email == identifier
                    : o.Mobile == identifier)
                .OrderByDescending(o => o.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (otpCode == null)
            {
                _logger.LogWarning(
                    "No OTP found for {Identifier} with purpose {Purpose}",
                    MaskIdentifier(identifier), purpose);
                return false;
            }

            // Check if OTP is expired
            if (otpCode.IsExpired)
            {
                _logger.LogWarning(
                    "OTP expired for {Identifier} with purpose {Purpose}",
                    MaskIdentifier(identifier), purpose);
                return false;
            }

            // Check if OTP is already verified
            if (otpCode.IsVerified)
            {
                _logger.LogWarning(
                    "OTP already verified for {Identifier} with purpose {Purpose}",
                    MaskIdentifier(identifier), purpose);
                return false;
            }

            // Check if max attempts reached
            if (otpCode.IsMaxAttemptsReached)
            {
                _logger.LogWarning(
                    "Max attempts reached for OTP {Identifier} with purpose {Purpose}",
                    MaskIdentifier(identifier), purpose);
                return false;
            }

            // Verify code
            if (otpCode.Code != hashedCode)
            {
                otpCode.IncrementAttempts();
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogWarning(
                    "Invalid OTP code attempt {Attempts}/{MaxAttempts} for {Identifier} with purpose {Purpose}",
                    otpCode.Attempts, otpCode.MaxAttempts,
                    MaskIdentifier(identifier), purpose);

                return false;
            }

            // Mark as verified
            otpCode.MarkAsVerified();
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "OTP successfully verified for {Identifier} with purpose {Purpose}",
                MaskIdentifier(identifier), purpose);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error verifying OTP for {Identifier} with purpose {Purpose}",
                MaskIdentifier(identifier), purpose);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<string> ResendOtpAsync(
        string identifier,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentNullException(nameof(identifier));

        if (string.IsNullOrWhiteSpace(purpose))
            throw new ArgumentNullException(nameof(purpose));

        try
        {
            var isEmail = identifier.Contains('@');

            // Check if recent OTP exists and is within resend delay
            var recentOtp = await _context.OtpCodes
                .Where(o => o.Purpose == purpose)
                .Where(o => isEmail
                    ? o.Email == identifier
                    : o.Mobile == identifier)
                .OrderByDescending(o => o.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (recentOtp != null)
            {
                var timeSinceLastOtp = _dateTime.UtcNow - recentOtp.CreatedAtUtc;
                var resendDelay = TimeSpan.FromMinutes(_settings.ResendDelayMinutes);

                if (timeSinceLastOtp < resendDelay)
                {
                    var remainingSeconds = (resendDelay - timeSinceLastOtp).TotalSeconds;
                    _logger.LogWarning(
                        "Resend attempt too soon for {Identifier}. Wait {Seconds} more seconds",
                        MaskIdentifier(identifier), (int)remainingSeconds);

                    throw new InvalidOperationException(
                        $"Please wait {(int)remainingSeconds} seconds before requesting a new code");
                }
            }

            // Generate new OTP
            return await GenerateOtpAsync(identifier, purpose, recentOtp?.UserId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error resending OTP for {Identifier} with purpose {Purpose}",
                MaskIdentifier(identifier), purpose);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task InvalidateOtpAsync(
        string identifier,
        string purpose,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            throw new ArgumentNullException(nameof(identifier));

        if (string.IsNullOrWhiteSpace(purpose))
            throw new ArgumentNullException(nameof(purpose));

        try
        {
            var isEmail = identifier.Contains('@');

            // Find all active OTPs for this identifier and purpose
            var otpCodes = await _context.OtpCodes
                .Where(o => o.Purpose == purpose)
                .Where(o => isEmail
                    ? o.Email == identifier
                    : o.Mobile == identifier)
                .Where(o => o.VerifiedAt == null)
                .ToListAsync(cancellationToken);

            if (otpCodes.Any())
            {
                // Mark as verified (invalidated)
                foreach (var otp in otpCodes)
                {
                    otp.MarkAsVerified();
                }

                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Invalidated {Count} OTP codes for {Identifier} with purpose {Purpose}",
                    otpCodes.Count, MaskIdentifier(identifier), purpose);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error invalidating OTP for {Identifier} with purpose {Purpose}",
                MaskIdentifier(identifier), purpose);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task CleanupExpiredOtpsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var expiredOtps = await _context.OtpCodes
                .Where(o => o.ExpiresAt < _dateTime.UtcNow)
                .ToListAsync(cancellationToken);

            if (expiredOtps.Any())
            {
                _context.OtpCodes.RemoveRange(expiredOtps);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogInformation(
                    "Cleaned up {Count} expired OTP codes",
                    expiredOtps.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cleaning up expired OTPs");
            throw;
        }
    }

    #region Private Methods

    /// <summary>
    /// Generate random numeric code
    /// </summary>
    private static string GenerateRandomCode(int length)
    {
        var randomNumber = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);

        var code = string.Empty;
        foreach (var b in randomNumber)
        {
            code += (b % 10).ToString();
        }

        return code[..length];
    }

    /// <summary>
    /// Hash OTP code using SHA256
    /// </summary>
    private static string HashCode(string code)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(code);
        var hash = SHA256.HashData(bytes);
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Mask identifier for logging (show first 2 and last 2 characters)
    /// </summary>
    private static string MaskIdentifier(string identifier)
    {
        if (identifier.Length <= 4)
            return "****";

        return $"{identifier[..2]}****{identifier[^2..]}";
    }

    #endregion
}
