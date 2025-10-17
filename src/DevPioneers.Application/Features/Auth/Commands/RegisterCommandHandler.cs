// ============================================
// File: DevPioneers.Application/Features/Auth/Commands/RegisterCommandHandler.cs
// ============================================
using DevPioneers.Application.Common.Exceptions;
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
            // Check if email already exists
            var emailExists = await _context.Users
                .AnyAsync(u => u.Email == request.Email, cancellationToken);

            if (emailExists)
            {
                return Result<AuthResponseDto>.Failure("Email address is already registered");
            }

            // Check if mobile already exists (if provided)
            if (!string.IsNullOrEmpty(request.Mobile))
            {
                var mobileExists = await _context.Users
                    .AnyAsync(u => u.Mobile == request.Mobile, cancellationToken);

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
                Mobile = request.Mobile,
                Status = UserStatus.Pending, // Will be Active after email verification
                CreatedAtUtc = _dateTime.UtcNow
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
            var wallet = new Wallet
            {
                UserId = user.Id,
                Balance = 0,
                Points = 0,
                Currency = "EGP",
                CreatedAtUtc = _dateTime.UtcNow
            };

            _context.Wallets.Add(wallet);
            await _context.SaveChangesAsync(cancellationToken);

            // Send welcome email with verification link
            try
            {
                await _emailService.SendWelcomeEmailAsync(user.Email, user.FullName, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send welcome email to {Email}", user.Email);
                // Don't fail registration if email fails
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

            _logger.LogInformation("User {UserId} registered successfully with email {Email}", user.Id, user.Email);
            return Result<AuthResponseDto>.Success(authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration failed for email {Email}", request.Email);
            return Result<AuthResponseDto>.Failure("An error occurred during registration");
        }
    }
}

// ============================================
// File: DevPioneers.Application/Features/Auth/Commands/RefreshTokenCommand.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Auth.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Auth.Commands;

public record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress = null,
    string? UserAgent = null
) : IRequest<Result<AuthResponseDto>>;

// ============================================
// File: DevPioneers.Application/Features/Auth/Commands/RefreshTokenCommandHandler.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Auth.DTOs;
using DevPioneers.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Auth.Commands;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;
    private readonly IDateTime _dateTime;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        ILogger<RefreshTokenCommandHandler> logger,
        IDateTime dateTime)
    {
        _context = context;
        _logger = logger;
        _dateTime = dateTime;
    }

    public async Task<Result<AuthResponseDto>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Find refresh token
            var refreshToken = await _context.RefreshTokens
                .Include(rt => rt.User)
                    .ThenInclude(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);

            if (refreshToken == null)
            {
                _logger.LogWarning("Refresh token not found: {RefreshToken}", request.RefreshToken[..8] + "...");
                return Result<AuthResponseDto>.Failure("Invalid refresh token");
            }

            // Check if token is expired
            if (refreshToken.IsExpired(_dateTime.UtcNow))
            {
                _logger.LogWarning("Expired refresh token used for user {UserId}", refreshToken.UserId);
                return Result<AuthResponseDto>.Failure("Refresh token has expired");
            }

            // Check if token is revoked
            if (refreshToken.IsRevoked)
            {
                _logger.LogWarning("Revoked refresh token used for user {UserId}", refreshToken.UserId);
                return Result<AuthResponseDto>.Failure("Refresh token has been revoked");
            }

            var user = refreshToken.User;

            // Check if user is still active
            if (user.Status != UserStatus.Active)
            {
                _logger.LogWarning("Refresh token used for inactive user {UserId}, Status: {Status}", 
                    user.Id, user.Status);
                return Result<AuthResponseDto>.Failure("User account is not active");
            }

            // Mark current token as used
            refreshToken.MarkAsUsed(_dateTime.UtcNow, request.IpAddress);
            await _context.SaveChangesAsync(cancellationToken);

            var authResponse = new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
                RequiresTwoFactor = false
                // New JWT tokens will be generated by Infrastructure layer
            };

            _logger.LogInformation("Refresh token used successfully for user {UserId}", user.Id);
            return Result<AuthResponseDto>.Success(authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh token operation failed");
            return Result<AuthResponseDto>.Failure("An error occurred during token refresh");
        }
    }
}

// ============================================
// File: DevPioneers.Application/Features/Auth/Commands/VerifyOtpCommand.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Auth.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Auth.Commands;

public record VerifyOtpCommand(
    string UserId,
    string OtpCode,
    string? IpAddress = null,
    string? UserAgent = null
) : IRequest<Result<AuthResponseDto>>;

// ============================================
// File: DevPioneers.Application/Features/Auth/Commands/VerifyOtpCommandHandler.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Auth.DTOs;
using DevPioneers.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Auth.Commands;

public class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, Result<AuthResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<VerifyOtpCommandHandler> _logger;
    private readonly IDateTime _dateTime;

    public VerifyOtpCommandHandler(
        IApplicationDbContext context,
        ILogger<VerifyOtpCommandHandler> logger,
        IDateTime dateTime)
    {
        _context = context;
        _logger = logger;
        _dateTime = dateTime;
    }

    public async Task<Result<AuthResponseDto>> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (!int.TryParse(request.UserId, out var userId))
            {
                return Result<AuthResponseDto>.Failure("Invalid user ID");
            }

            // Find user with their OTP codes
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.OtpCodes.Where(otp => !otp.IsUsed && otp.ExpiresAt > DateTime.UtcNow))
                .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("OTP verification failed: User not found {UserId}", userId);
                return Result<AuthResponseDto>.Failure("Invalid user");
            }

            // Find valid OTP code
            var otpCode = user.OtpCodes
                .FirstOrDefault(otp => otp.Code == request.OtpCode && 
                                      !otp.IsUsed && 
                                      otp.ExpiresAt > _dateTime.UtcNow);

            if (otpCode == null)
            {
                _logger.LogWarning("OTP verification failed: Invalid or expired code for user {UserId}", userId);
                return Result<AuthResponseDto>.Failure("Invalid or expired OTP code");
            }

            // Mark OTP as used
            otpCode.MarkAsUsed(_dateTime.UtcNow);

            // Update user login info
            user.RecordSuccessfulLogin(_dateTime.UtcNow, request.IpAddress);

            await _context.SaveChangesAsync(cancellationToken);

            var authResponse = new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
                RequiresTwoFactor = false
                // JWT tokens will be generated by Infrastructure layer
            };

            _logger.LogInformation("OTP verified successfully for user {UserId}", user.Id);
            return Result<AuthResponseDto>.Success(authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OTP verification failed for user {UserId}", request.UserId);
            return Result<AuthResponseDto>.Failure("An error occurred during OTP verification");
        }
    }
}

// ============================================
// File: DevPioneers.Application/Features/Auth/Commands/SendOtpCommand.cs
// ============================================
using DevPioneers.Application.Common.Models;
using MediatR;

namespace DevPioneers.Application.Features.Auth.Commands;

public record SendOtpCommand(
    string EmailOrMobile,
    OtpPurpose Purpose = OtpPurpose.TwoFactorAuth
) : IRequest<Result<string>>; // Returns masked email/mobile

public enum OtpPurpose
{
    TwoFactorAuth,
    EmailVerification,
    PasswordReset,
    MobileVerification
}

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
                await _emailService.SendOtpEmailAsync(user.Email, otpCode, user.FullName, cancellationToken);
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
