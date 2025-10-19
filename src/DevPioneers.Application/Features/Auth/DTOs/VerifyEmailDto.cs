// ============================================
// File: DevPioneers.Application/Features/Auth/DTOs/VerifyEmailDto.cs
// ============================================
using System.ComponentModel.DataAnnotations;

namespace DevPioneers.Application.Features.Auth.DTOs;

public class VerifyEmailDto
{
    [Required(ErrorMessage = "Verification token is required")]
    public string Token { get; set; } = string.Empty;
}