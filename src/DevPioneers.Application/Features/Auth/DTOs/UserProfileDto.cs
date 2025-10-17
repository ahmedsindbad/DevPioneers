// ============================================
// File: DevPioneers.Application/Features/Auth/DTOs/UserProfileDto.cs
// ============================================
using DevPioneers.Application.Common.Models;

namespace DevPioneers.Application.Features.Auth.DTOs;

public class UserProfileDto : BaseDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public bool MobileVerified { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public List<RoleDto> Roles { get; set; } = new();
    public decimal WalletBalance { get; set; }
    public int WalletPoints { get; set; }
    public SubscriptionSummaryDto? ActiveSubscription { get; set; }
}
