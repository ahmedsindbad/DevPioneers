// ============================================
// File: DevPioneers.Application/Features/Auth/Commands/SendOtpCommandHandler.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Domain.Entities;
using DevPioneers.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Auth.Commands;

public class SendOtpCommandHandler : IRequestHandler<SendOtpCommand, Result<string>>
{
    private readonly IApplicationDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<SendOtpCommandHandler> _logger;
    private readonly IDateTime _dateTime;

    public SendOtpCommandHandler(
        IApplicationDbContext context,
        IEmailService emailService,
        ILogger<SendOtpCommandHandler> logger,
        IDateTime dateTime)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
        _dateTime = dateTime;
    }

    public async Task<Result<string>> Handle(SendOtpCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Find user
            var user = await _context.Users
                .FirstOrDefaultAsync(u => 
                    u.Email == request.EmailOrMobile || 
                    u.Mobile == request.EmailOrMobile, 
                    cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("OTP send failed: User not found for {EmailOrMobile}", request.EmailOrMobile);
                return Result<string>.Failure("User not found");
            }

            // Check rate limiting (max 3 OTPs per 15 minutes)
            var recentOtps = await _context.OtpCodes
                .Where(otp => otp.UserId == user.Id && 
                             otp.CreatedAtUtc > _dateTime.UtcNow.AddMinutes(-15))
                .CountAsync(cancellationToken);

            if (recentOtps >= 3)
            {
                _logger.LogWarning("OTP rate limit exceeded for user {UserId}", user.Id);
                return Result<string>.Failure("Too many OTP requests. Please wait 15 minutes.");
            }

            // Generate OTP
            var otpCode = GenerateOtpCode();
            var expiresAt = _dateTime.UtcNow.AddMinutes(5); // 5 minutes expiry

            var otp = new OtpCode
            {
                UserId = user.Id,
                Code = otpCode,
                Purpose = request.Purpose.ToString(),
                ExpiresAt = expiresAt,
                CreatedAtUtc = _dateTime.UtcNow
            };

            _context.OtpCodes.Add(otp);
            await _context.SaveChangesAsync(cancellationToken);

            // Send OTP via email
            try
            {
                await _emailService.SendOtpEmailAsync(user.Email, otpCode, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send OTP email to user {UserId}", user.Id);
                return Result<string>.Failure("Failed to send OTP. Please try again.");
            }

            var maskedEmail = MaskEmail(user.Email);
            
            _logger.LogInformation("OTP sent successfully to user {UserId}", user.Id);
            return Result<string>.Success(maskedEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Send OTP failed for {EmailOrMobile}", request.EmailOrMobile);
            return Result<string>.Failure("An error occurred while sending OTP");
        }
    }

    private static string GenerateOtpCode()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    private static string MaskEmail(string email)
    {
        var parts = email.Split('@');
        if (parts.Length != 2) return email;

        var username = parts[0];
        var domain = parts[1];

        if (username.Length <= 2)
            return $"{username[0]}***@{domain}";

        return $"{username[0]}***{username[^1]}@{domain}";
    }
}
