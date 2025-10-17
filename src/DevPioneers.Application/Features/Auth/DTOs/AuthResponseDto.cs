// ============================================
// File: DevPioneers.Application/Features/Auth/DTOs/AuthResponseDto.cs
// ============================================
namespace DevPioneers.Application.Features.Auth.DTOs;

public class AuthResponseDto
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = new();
    public bool RequiresTwoFactor { get; set; }
    public bool RequiresEmailVerification { get; set; }
    public string? TwoFactorUserId { get; set; }
    
    // JWT tokens (will be set by Infrastructure layer)
    public string? AccessToken { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? AccessTokenExpires { get; set; }
    public DateTime? RefreshTokenExpires { get; set; }
}
