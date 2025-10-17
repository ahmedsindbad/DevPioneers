// ============================================
// File: DevPioneers.Application/Common/Mappings/UserMappings.cs
// ============================================
using DevPioneers.Application.Features.Auth.DTOs;
using DevPioneers.Domain.Entities;

namespace DevPioneers.Application.Common.Mappings;

public static class UserMappings
{
    public static UserProfileDto ToProfileDto(this User user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            Mobile = user.Mobile,
            Status = user.Status.ToString(),
            EmailVerified = user.EmailVerified,
            MobileVerified = user.MobileVerified,
            TwoFactorEnabled = user.TwoFactorEnabled,
            LastLoginAt = user.LastLoginAt,
            CreatedAtUtc = user.CreatedAtUtc,
            UpdatedAtUtc = user.UpdatedAtUtc
        };
    }

    public static AuthResponseDto ToAuthResponseDto(this User user)
    {
        return new AuthResponseDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName,
            RequiresTwoFactor = user.TwoFactorEnabled,
            RequiresEmailVerification = !user.EmailVerified
        };
    }
}
