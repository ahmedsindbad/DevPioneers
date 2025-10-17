// ============================================
// File: DevPioneers.Application/Features/Auth/Queries/GetUserProfileQueryHandler.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Auth.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Auth.Queries;

public class GetUserProfileQueryHandler : IRequestHandler<GetUserProfileQuery, Result<UserProfileDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetUserProfileQueryHandler> _logger;

    public GetUserProfileQueryHandler(
        IApplicationDbContext context,
        ILogger<GetUserProfileQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<UserProfileDto>> Handle(GetUserProfileQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

            if (user == null)
            {
                return Result<UserProfileDto>.Failure("User not found");
            }

            var profile = new UserProfileDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Mobile = user.Mobile,
                Status = user.Status.ToString(),
                EmailVerified = user.EmailVerified,
                MobileVerified = user.MobileVerified,
                TwoFactorEnabled = user.TwoFactorEnabled,
                LastLoginAt = user.LastLoginAt,
                CreatedAtUtc = user.CreatedAtUtc
            };

            return Result<UserProfileDto>.Success(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user profile for user {UserId}", request.UserId);
            return Result<UserProfileDto>.Failure("An error occurred while retrieving user profile");
        }
    }
}
