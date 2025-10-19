// ============================================
// File: DevPioneers.Application/Features/Auth/Commands/ResendEmailVerificationCommandHandler.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Auth.Commands;

public class ResendEmailVerificationCommandHandler : IRequestHandler<ResendEmailVerificationCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ResendEmailVerificationCommandHandler> _logger;
    private readonly IDateTime _dateTime;
    private readonly IEmailService _emailService;

    public ResendEmailVerificationCommandHandler(
        IApplicationDbContext context,
        ILogger<ResendEmailVerificationCommandHandler> logger,
        IDateTime dateTime,
        IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _dateTime = dateTime;
        _emailService = emailService;
    }

    public async Task<Result<bool>> Handle(ResendEmailVerificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email, cancellationToken);

            if (user == null)
            {
                // Don't reveal if email exists or not for security
                _logger.LogWarning("Resend verification attempt for non-existent email {Email}", request.Email);
                return Result<bool>.Success(true); // Return success to prevent email enumeration
            }

            // Check if email is already verified
            if (user.EmailVerified)
            {
                return Result<bool>.Failure("Email address is already verified");
            }

            // Check if user is blocked or deleted
            if (user.Status == UserStatus.Banned || user.Status == UserStatus.Deactivated)
            {
                return Result<bool>.Failure("Account is not available for verification");
            }

            // Check rate limiting (prevent spam)
            var recentVerificationAttempt = await _context.AuditTrails
                .Where(a => a.EntityName == "User" && 
                           a.Action == AuditAction.EmailVerificationSent &&
                           a.UserId == user.Id &&
                           a.Timestamp > _dateTime.UtcNow.AddMinutes(-2)) // 2 minute cooldown
                .AnyAsync(cancellationToken);

            if (recentVerificationAttempt)
            {
                return Result<bool>.Failure("Please wait before requesting another verification email");
            }

            // Generate new verification token
            user.GenerateEmailVerificationToken();

            // Save changes
            await _context.SaveChangesAsync(cancellationToken);

            // Send verification email
            try
            {
                var verificationUrl = $"https://yourdomain.com/verify-email?token={user.EmailVerificationToken}";
                await _emailService.SendEmailVerificationAsync(user.Email, user.FullName, verificationUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to {Email}", user.Email);
                return Result<bool>.Failure("Failed to send verification email. Please try again.");
            }

            _logger.LogInformation("Verification email resent to user {UserId} ({Email})", user.Id, user.Email);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Resend verification failed for email {Email}", request.Email);
            return Result<bool>.Failure("An error occurred while sending verification email");
        }
    }
}
