// ============================================
// File: DevPioneers.Application/Features/Auth/DTOs/LoginDto.cs
// ============================================
namespace DevPioneers.Application.Features.Auth.DTOs;

public class LoginDto
{
    public string EmailOrMobile { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; }
}
