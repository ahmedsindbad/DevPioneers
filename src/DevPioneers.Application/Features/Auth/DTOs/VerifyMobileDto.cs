// ============================================
// File: DevPioneers.Application/Features/Auth/DTOs/VerifyMobileDto.cs
// ============================================
using System.ComponentModel.DataAnnotations;

namespace DevPioneers.Application.Features.Auth.DTOs;

public class VerifyMobileDto
{
    [Required(ErrorMessage = "Mobile number is required")]
    [Phone(ErrorMessage = "Please enter a valid mobile number")]
    public string Mobile { get; set; } = string.Empty;

    [Required(ErrorMessage = "OTP code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP code must be 6 digits")]
    [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP code must contain only numbers")]
    public string OtpCode { get; set; } = string.Empty;
}