// ============================================
// File: DevPioneers.Application/Features/Auth/Commands/SendOtpCommand.cs
// ============================================
using DevPioneers.Application.Common.Models;
using MediatR;

namespace DevPioneers.Application.Features.Auth.Commands;

public record SendOtpCommand(
    string EmailOrMobile,
    OtpPurpose Purpose = OtpPurpose.TwoFactorAuth
) : IRequest<Result<string>>; // Returns masked email/mobile

public enum OtpPurpose
{
    TwoFactorAuth,
    EmailVerification,
    PasswordReset,
    MobileVerification
}
