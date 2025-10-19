// ============================================
// File: DevPioneers.Application/Features/Auth/DTOs/ResendVerificationDto.cs
// ============================================
using System.ComponentModel.DataAnnotations;

namespace DevPioneers.Application.Features.Auth.DTOs;

public class ResendVerificationDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    public string Email { get; set; } = string.Empty;
}