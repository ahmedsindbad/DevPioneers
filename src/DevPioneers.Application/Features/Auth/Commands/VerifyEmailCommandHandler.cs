// ============================================
// File: DevPioneers.Application/Features/Auth/Commands/VerifyEmailCommandHandler.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Auth.Commands;

public class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<VerifyEmailCommandHandler> _logger;
    private readonly IDateTime _dateTime;

    public VerifyEmailCommandHandler(
        IApplicationDbContext context,
        ILogger<VerifyEmailCommandHandler> logger,
        IDateTime dateTime)
    {
        _context = context;
        _logger = logger;
        _dateTime = dateTime;
    }

    public async Task<Result<bool>> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Token))
            {
                return Result<bool>.Failure("Invalid verification token");
            }

            // Find user with the verification token
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.EmailVerificationToken == request.Token && 
                                         !u.EmailVerified &&
                                         u.EmailVerificationTokenExpiresAt > DateTime.UtcNow, 
                                   cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Email verification failed: Invalid or expired token {Token}", request.Token);
                return Result<bool>.Failure("Invalid or expired verification token");
            }

            // Verify the email
            user.VerifyEmail();
            
            // If user was pending, activate the account
            if (user.Status == UserStatus.Pending)
            {
                user.Status = UserStatus.Active;
                user.LastLoginAt = _dateTime.UtcNow;
            }

            // Record the verification
            user.EmailVerifiedAt = _dateTime.UtcNow;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Email verified successfully for user {UserId} ({Email})", 
                user.Id, user.Email);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email verification failed for token {Token}", request.Token);
            return Result<bool>.Failure("An error occurred during email verification");
        }
    }
}
