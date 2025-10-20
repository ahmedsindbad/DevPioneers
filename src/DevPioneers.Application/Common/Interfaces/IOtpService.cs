// ============================================
// File: DevPioneers.Application/Common/Interfaces/IOtpService.cs
// ============================================
namespace DevPioneers.Application.Common.Interfaces;

/// <summary>
/// OTP service interface for one-time password operations
/// </summary>
public interface IOtpService
{
    /// <summary>
    /// Generate and send OTP code
    /// </summary>
    Task<string> GenerateOtpAsync(
        string identifier,
        string purpose,
        int? userId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify OTP code
    /// </summary>
    Task<bool> VerifyOtpAsync(
        string identifier,
        string code,
        string purpose,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resend OTP code
    /// </summary>
    Task<string> ResendOtpAsync(
        string identifier,
        string purpose,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate all OTP codes for an identifier
    /// </summary>
    Task InvalidateOtpAsync(
        string identifier,
        string purpose,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clean up expired OTP codes
    /// </summary>
    Task CleanupExpiredOtpsAsync(CancellationToken cancellationToken = default);
}
