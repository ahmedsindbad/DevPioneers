// ============================================
// File: DevPioneers.Application/Features/Auth/DTOs/RegisterDto.cs
// ============================================
namespace DevPioneers.Application.Features.Auth.DTOs;

public class RegisterDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Mobile { get; set; }
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public bool AcceptTerms { get; set; }
}
