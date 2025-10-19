// ============================================
// File: DevPioneers.Application/Features/Auth/Validators/RegisterCommandValidator.cs
// ============================================
using DevPioneers.Application.Features.Auth.Commands;
using FluentValidation;

namespace DevPioneers.Application.Features.Auth.Validators;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("Full name is required")
            .Length(2, 100)
            .WithMessage("Full name must be between 2 and 100 characters")
            .Matches(@"^[a-zA-Z\u0600-\u06FF\s]+$")
            .WithMessage("Full name can only contain letters and spaces");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Please enter a valid email address")
            .MaximumLength(320)
            .WithMessage("Email address is too long");

        RuleFor(x => x.Mobile)
            .Must(BeValidMobileOrEmpty)
            .WithMessage("Mobile number must be a valid Egyptian number (01xxxxxxxxx or +201xxxxxxxxx)")
            .When(x => !string.IsNullOrWhiteSpace(x.Mobile));

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long")
            .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$")
            .WithMessage("Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character");
    }

    private static bool BeValidMobileOrEmpty(string? mobile)
    {
        if (string.IsNullOrWhiteSpace(mobile))
            return true;

        mobile = mobile.Trim().Replace(" ", "").Replace("-", "");
        
        if (mobile.StartsWith("+20"))
            mobile = mobile[3..];
        
        return mobile.Length == 11 && 
               mobile.StartsWith("01") && 
               mobile.All(char.IsDigit);
    }
}