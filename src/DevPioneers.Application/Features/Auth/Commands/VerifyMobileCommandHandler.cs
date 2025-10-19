// ============================================
// File: DevPioneers.Application/Features/Auth/Commands/VerifyMobileCommandHandler.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Auth.Commands;

public class VerifyMobileCommandHandler : IRequestHandler<VerifyMobileCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<VerifyMobileCommandHandler> _logger;
    private readonly IDateTime _dateTime;

    public VerifyMobileCommandHandler(
        IApplicationDbContext context,
        ILogger<VerifyMobileCommandHandler> logger,
        IDateTime dateTime)
    {
        _context = context;
        _logger = logger;
        _dateTime = dateTime;
    }

    public async Task<Result<bool>> Handle(VerifyMobileCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Normalize mobile number
            var normalizedMobile = NormalizeMobile(request.Mobile);
            
            // Find user by mobile
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Mobile == normalizedMobile, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Mobile verification failed: User not found for mobile {Mobile}", normalizedMobile);
                return Result<bool>.Failure("Invalid mobile number");
            }

            // Find valid OTP code for this mobile
            var otpCode = await _context.OtpCodes
                .Where(otp => otp.Mobile == normalizedMobile &&
                             otp.Code == request.OtpCode &&
                             !otp.IsVerified &&
                             otp.ExpiresAt > _dateTime.UtcNow &&
                             (otp.Purpose == OtpCode.Purposes.Registration || 
                              otp.Purpose == OtpCode.Purposes.PhoneVerification))
                .OrderByDescending(otp => otp.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (otpCode == null)
            {
                _logger.LogWarning("Mobile verification failed: Invalid or expired OTP for mobile {Mobile}", normalizedMobile);
                return Result<bool>.Failure("Invalid or expired OTP code");
            }

            // Increment attempt count
            otpCode.IncrementAttempts();

            // Check if max attempts reached
            if (otpCode.IsMaxAttemptsReached)
            {
                _logger.LogWarning("Mobile verification failed: Max attempts reached for mobile {Mobile}", normalizedMobile);
                await _context.SaveChangesAsync(cancellationToken);
                return Result<bool>.Failure("Maximum verification attempts reached. Please request a new OTP.");
            }

            // Verify the OTP
            otpCode.MarkAsVerified();

            // Verify user's mobile
            user.VerifyMobile();

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Mobile verified successfully for user {UserId} - Mobile: {Mobile}", 
                user.Id, normalizedMobile);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Mobile verification failed for mobile {Mobile}", request.Mobile);
            return Result<bool>.Failure("An error occurred during mobile verification");
        }
    }

    private static string NormalizeMobile(string mobile)
    {
        mobile = mobile.Trim().Replace(" ", "").Replace("-", "");
        
        if (mobile.StartsWith("+20"))
            mobile = mobile[3..];

        return mobile;
    }
}