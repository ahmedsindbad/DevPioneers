// ============================================
// File: DevPioneers.Application/Features/Auth/DTOs/OtpDto.cs
// ============================================
namespace DevPioneers.Application.Features.Auth.DTOs;

public class OtpDto
{
    public string UserId { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
}
