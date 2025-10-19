// ============================================
// File: DevPioneers.Application/Features/Auth/Validators/VerifyEmailCommandValidator.cs
// ============================================
using DevPioneers.Application.Features.Auth.Commands;
using FluentValidation;

namespace DevPioneers.Application.Features.Auth.Validators;

public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty()
            .WithMessage("Verification token is required")
            .Length(64)
            .WithMessage("Invalid verification token format")
            .Matches(@"^[a-zA-Z0-9]+$")
            .WithMessage("Verification token contains invalid characters");
    }
}