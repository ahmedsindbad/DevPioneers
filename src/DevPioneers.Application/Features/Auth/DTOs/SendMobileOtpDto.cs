// ============================================
// File: DevPioneers.Application/Features/Auth/DTOs/SendMobileOtpDto.cs
// ============================================
using System.ComponentModel.DataAnnotations;

namespace DevPioneers.Application.Features.Auth.DTOs;

public class SendMobileOtpDto
{
    [Required(ErrorMessage = "Mobile number is required")]
    [Phone(ErrorMessage = "Please enter a valid mobile number")]
    [RegularExpression(@"^(\+201|01)[0-9]{9}$", ErrorMessage = "Mobile number must be a valid Egyptian number")]
    public string Mobile { get; set; } = string.Empty;

    [Required(ErrorMessage = "Purpose is required")]
    public string Purpose { get; set; } = "Registration";
}