// ============================================
// File: DevPioneers.Application/Features/Auth/Validators/VerifyOtpCommandValidator.cs
// ============================================
using DevPioneers.Application.Features.Auth.Commands;
using FluentValidation;

namespace DevPioneers.Application.Features.Auth.Validators;

public class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required")
            .Must(BeValidUserId)
            .WithMessage("Invalid user ID format");

        RuleFor(x => x.OtpCode)
            .NotEmpty()
            .WithMessage("OTP code is required")
            .Length(6)
            .WithMessage("OTP code must be exactly 6 digits")
            .Matches(@"^\d{6}$")
            .WithMessage("OTP code must contain only numbers");
    }

    private static bool BeValidUserId(string userId)
    {
        return int.TryParse(userId, out var id) && id > 0;
    }
}