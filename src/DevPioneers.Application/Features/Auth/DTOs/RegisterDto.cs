// ============================================
// File: DevPioneers.Application/Features/Auth/DTOs/RegisterDto.cs
// Updated with comprehensive validation
// ============================================
using System.ComponentModel.DataAnnotations;

namespace DevPioneers.Application.Features.Auth.DTOs;

public class RegisterDto
{
    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 100 characters")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(320, ErrorMessage = "Email address is too long")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Please enter a valid mobile number")]
    [RegularExpression(@"^(\+201|01)[0-9]{9}$", ErrorMessage = "Mobile number must be a valid Egyptian number (01xxxxxxxxx or +201xxxxxxxxx)")]
    public string? Mobile { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$", 
        ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password confirmation is required")]
    [Compare("Password", ErrorMessage = "Password and confirmation password do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "You must accept the terms and conditions")]
    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms and conditions")]
    public bool AcceptTerms { get; set; }

    /// <summary>
    /// Optional: Marketing emails consent
    /// </summary>
    public bool AcceptMarketing { get; set; } = false;
}

// // ============================================
// // File: DevPioneers.Application/Features/Auth/DTOs/RegisterDto.cs
// // ============================================
// namespace DevPioneers.Application.Features.Auth.DTOs;

// public class RegisterDto
// {
//     public string FullName { get; set; } = string.Empty;
//     public string Email { get; set; } = string.Empty;
//     public string? Mobile { get; set; }
//     public string Password { get; set; } = string.Empty;
//     public string ConfirmPassword { get; set; } = string.Empty;
//     public bool AcceptTerms { get; set; }
// }
