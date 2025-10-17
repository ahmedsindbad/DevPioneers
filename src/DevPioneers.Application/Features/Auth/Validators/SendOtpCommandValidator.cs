// ============================================
// File: DevPioneers.Application/Features/Auth/Validators/SendOtpCommandValidator.cs
// ============================================
using DevPioneers.Application.Features.Auth.Commands;
using FluentValidation;

namespace DevPioneers.Application.Features.Auth.Validators;

public class SendOtpCommandValidator : AbstractValidator<SendOtpCommand>
{
    public SendOtpCommandValidator()
    {
        RuleFor(x => x.EmailOrMobile)
            .NotEmpty()
            .WithMessage("Email or mobile number is required")
            .Must(BeValidEmailOrMobile)
            .WithMessage("Please provide a valid email address or mobile number");

        RuleFor(x => x.Purpose)
            .IsInEnum()
            .WithMessage("Invalid OTP purpose");
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

        // Check if it's a valid mobile
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
        mobile = mobile.Trim().Replace(" ", "").Replace("-", "");
        
        if (mobile.StartsWith("+20"))
            mobile = mobile[3..];
        
        return mobile.Length == 11 && 
               mobile.StartsWith("01") && 
               mobile.All(char.IsDigit);
    }
}