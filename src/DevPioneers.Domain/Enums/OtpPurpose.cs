// ============================================
// File: DevPioneers.Domain/Enums/OtpPurpose.cs
// ============================================
namespace DevPioneers.Domain.Enums;

/// <summary>
/// OTP purpose enumeration
/// </summary>
public enum OtpPurpose
{
    Login = 1,
    Registration = 2,
    PasswordReset = 3,
    PhoneVerification = 4,
    EmailVerification = 5,
    TwoFactorAuth = 6
}
