// ============================================
// File: DevPioneers.Application/Features/Auth/DTOs/ChangePasswordDto.cs
// ============================================
namespace DevPioneers.Application.Features.Auth.DTOs;

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}
