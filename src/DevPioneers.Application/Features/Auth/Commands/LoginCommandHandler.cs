// ============================================
// File: DevPioneers.Application/Features/Auth/Commands/LoginCommandHandler.cs
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

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponseDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<LoginCommandHandler> _logger;
    private readonly IDateTime _dateTime;

    public LoginCommandHandler(
        IApplicationDbContext context,
        ILogger<LoginCommandHandler> logger,
        IDateTime dateTime)
    {
        _context = context;
        _logger = logger;
        _dateTime = dateTime;
    }

    public async Task<Result<AuthResponseDto>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Find user by email or mobile
            var user = await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u =>
                    u.Email == request.EmailOrMobile ||
                    u.Mobile == request.EmailOrMobile,
                    cancellationToken);

            if (user == null)
            {
                _logger.LogWarning("Login attempt failed: User not found for {EmailOrMobile}", request.EmailOrMobile);
                return Result<AuthResponseDto>.Failure("Invalid email/mobile or password");
            }

            // Check if account is locked
            if (user.IsLocked())
            {
                _logger.LogWarning("Login attempt failed: Account locked for user {UserId}", user.Id);
                return Result<AuthResponseDto>.Failure($"Account is locked until {user.LockedUntil:yyyy-MM-dd HH:mm}");
            }

            // Check if account is active
            if (user.Status != UserStatus.Active)
            {
                _logger.LogWarning("Login attempt failed: Account not active for user {UserId}, Status: {Status}",
                    user.Id, user.Status);
                return Result<AuthResponseDto>.Failure("Account is not active");
            }

            // Verify password
            if (!(user.PasswordHash == HashPassword(request.Password)))
            {
                // Increment failed login attempts
                user.RecordFailedLogin(_dateTime.UtcNow);
                await _context.SaveChangesAsync(cancellationToken);

                _logger.LogWarning("Login attempt failed: Invalid password for user {UserId}", user.Id);
                return Result<AuthResponseDto>.Failure("Invalid email/mobile or password");
            }

            // Check if 2FA is enabled
            if (user.TwoFactorEnabled)
            {
                _logger.LogInformation("2FA required for user {UserId}", user.Id);
                return Result<AuthResponseDto>.Success(new AuthResponseDto
                {
                    RequiresTwoFactor = true,
                    TwoFactorUserId = user.Id.ToString()
                });
            }

            // Successful login
            user.RecordSuccessfulLogin(_dateTime.UtcNow, request.IpAddress);
            await _context.SaveChangesAsync(cancellationToken);

            var authResponse = new AuthResponseDto
            {
                UserId = user.Id,
                Email = user.Email,
                FullName = user.FullName,
                Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
                RequiresTwoFactor = false
                // JWT tokens will be added by Infrastructure layer
            };

            _logger.LogInformation("User {UserId} logged in successfully", user.Id);
            return Result<AuthResponseDto>.Success(authResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login failed for {EmailOrMobile}", request.EmailOrMobile);
            return Result<AuthResponseDto>.Failure("An error occurred during login");
        }
    }
    private string HashPassword(string password)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        byte[] hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashedBytes);
    }
}