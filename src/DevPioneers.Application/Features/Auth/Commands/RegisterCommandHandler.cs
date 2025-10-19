// ============================================
// File: DevPioneers.Application/Features/Auth/Commands/RegisterCommandHandler.cs
// Updated version with mobile OTP support
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Auth.DTOs;
using DevPioneers.Domain.Entities;
using DevPioneers.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Auth.Commands;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<AuthResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<RegisterCommandHandler> _logger;
    private readonly IDateTime _dateTime;
    private readonly IEmailService _emailService;

    public RegisterCommandHandler(
        IApplicationDbContext context,
        ILogger<RegisterCommandHandler> logger,
        IDateTime dateTime,
        IEmailService emailService)
    {
        _context = context;
        _logger = logger;
        _dateTime = dateTime;
        _emailService = emailService;
    }

    public async Task<Result<AuthResponseDto>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Normalize mobile if provided
            var normalizedMobile = !string.IsNullOrEmpty(request.Mobile) 
                ? NormalizeMobile(request.Mobile) 
                : null;

            // Check if email already exists
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == request.Email, cancellationToken);

            if (emailExists)
            {
                return Result<AuthResponseDto>.Failure("Email address is already registered");
            }

            // Check if mobile already exists (if provided)
            if (!string.IsNullOrEmpty(normalizedMobile))
            {
                var mobileExists = await _context.Users
                    .AnyAsync(u => u.Mobile == normalizedMobile, cancellationToken);

                if (mobileExists)
                {
                    return Result<AuthResponseDto>.Failure("Mobile number is already registered");
                }
            }

            // Get default "User" role
            var userRole = await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == "User", cancellationToken);

            if (userRole == null)
            {
                _logger.LogError("Default 'User' role not found during registration");
                return Result<AuthResponseDto>.Failure("System error: Default role not found");
            }

            // Create new user
            var user = new User
            {
                FullName = request.FullName,
                Email = request.Email,
                Mobile = normalizedMobile,
                Status = UserStatus.Pending, // Will be Active after email verification
                CreatedAtUtc = _dateTime.UtcNow,
                RegistrationIpAddress = request.IpAddress
            };

            // Set password
            user.SetPassword(request.Password);

            // Generate email verification token
            user.GenerateEmailVerificationToken();

            // Add to database
            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);

            // Assign default role
            var userRoleAssignment = new UserRole
            {
                UserId = user.Id,
                RoleId = userRole.Id,
                CreatedAtUtc = _dateTime.UtcNow
            };

            _context.UserRoles.Add(userRoleAssignment);

            // Create wallet for user
            var wallet = new DevPioneers.Domain.Entities.Wallet
            {
                UserId = user.Id,
                Balance = 0,
                Points = 0,
                Currency = "EGP",
                CreatedAtUtc = _dateTime.UtcNow
            };

            _context.Wallets.Add(wallet);

            // Generate mobile OTP if mobile is provided
            if (!string.IsNullOrEmpty(normalizedMobile))
            {
                var mobileOtpCode = GenerateOtpCode();
                var mobileOtp = new OtpCode
                {
                    UserId = user.Id,
                    Mobile = normalizedMobile,
                    Code = mobileOtpCode,
                    Purpose = OtpCode.Purposes.Registration,
                    ExpiresAt = _dateTime.UtcNow.AddMinutes(10),
                    CreatedAtUtc = _dateTime.UtcNow,
                    IpAddress = request.IpAddress
                };

                _context.OtpCodes.Add(mobileOtp);
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Send welcome email with verification link
            try
            {
                var verificationUrl = $"https://yourdomain.com/verify-email?token={user.EmailVerificationToken}";
                await _emailService.SendEmailVerificationAsync(user.Email, user.FullName, verificationUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send verification email to {Email}", user.Email);
                // Don't fail registration if email fails
            }

            // Send mobile OTP if mobile is provided
            if (!string.IsNullOrEmpty(normalizedMobile))
            {
                try
                {
                    var mobileOtp = await _context.OtpCodes
                        .Where(otp => otp.UserId == user.Id && 
                                     otp.Mobile == normalizedMobile && 
                                     otp.Purpose == OtpCode.Purposes.Registration)
                        .OrderByDescending(otp => otp.CreatedAtUtc)
                        .FirstOrDefaultAsync(cancellationToken);

                    if (mobileOtp != null)
                    {
                        await _emailService.SendMobileVerificationOtpAsync(normalizedMobile, mobileOtp.Code, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send mobile OTP to {Mobile}", normalizedMobile);
                    // Don't fail registration if SMS fails
                }
            }

            var authResponse = new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Roles = new List<string> { "User" },
                RequiresTwoFactor = false,
                RequiresEmailVerification = true
            };

            _logger.LogInformation("User {UserId} registered successfully with email {Email} and mobile {Mobile}", 
                user.Id, user.Email, normalizedMobile ?? "Not provided");
            
            return Result<AuthResponseDto>.Success(authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for email {Email}", request.Email);
            return Result<AuthResponseDto>.Failure("An error occurred during registration");
        }
    }

    private static string GenerateOtpCode()
    {
        var random = new Random();
        return random.Next(100000, 999999).ToString();
    }

    private static string NormalizeMobile(string mobile)
    {
        mobile = mobile.Trim().Replace(" ", "").Replace("-", "");
        
        if (mobile.StartsWith("+20"))
            mobile = mobile[3..];

        return mobile;
    }
}