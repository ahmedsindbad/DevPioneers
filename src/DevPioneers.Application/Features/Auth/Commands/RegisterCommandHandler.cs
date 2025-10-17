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
            var wallet = new Domain.Entities.Wallet
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









