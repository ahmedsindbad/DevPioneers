// ============================================
// File: DevPioneers.Application/Features/Auth/Validators/LoginCommandValidator.cs
// ============================================
using DevPioneers.Application.Features.Auth.Commands;
using FluentValidation;

namespace DevPioneers.Application.Features.Auth.Validators;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.EmailOrMobile)
            .NotEmpty()
            .WithMessage("Email or mobile number is required")
            .Must(BeValidEmailOrMobile)
            .WithMessage("Please provide a valid email address or mobile number");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(6)
            .WithMessage("Password must be at least 6 characters long");
    }

    private static bool BeValidEmailOrMobile(string emailOrMobile)
    {
        if (string.IsNullOrWhiteSpace(emailOrMobile))
            return false;

        // Check if it's a valid email
        if (emailOrMobile.Contains('@'))
        {
            return IsValidEmail(emailOrMobile);
        }

        // Check if it's a valid mobile (Egyptian format)
        return IsValidMobile(emailOrMobile);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    private static bool IsValidMobile(string mobile)
    {
        // Egyptian mobile format: 01xxxxxxxxx (11 digits)
        mobile = mobile.Trim().Replace(" ", "").Replace("-", "");
        
        if (mobile.StartsWith("+20"))
            mobile = mobile[3..];
        
        return mobile.Length == 11 && 
               mobile.StartsWith("01") && 
               mobile.All(char.IsDigit);
    }
}
