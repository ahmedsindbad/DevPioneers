// ============================================
// File: DevPioneers.Application/Features/Auth/Commands/CheckUserExistsCommandHandler.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Auth.Commands;

public class CheckUserExistsCommandHandler : IRequestHandler<CheckUserExistsCommand, Result<bool>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CheckUserExistsCommandHandler> _logger;

    public CheckUserExistsCommandHandler(
        IApplicationDbContext context,
        ILogger<CheckUserExistsCommandHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(CheckUserExistsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var emailOrMobile = request.EmailOrMobile.Trim();
            bool userExists;

            if (emailOrMobile.Contains('@'))
            {
                // Check by email
                userExists = await _context.Users
                    .AnyAsync(u => u.Email == emailOrMobile, cancellationToken);
            }
            else
            {
                // Check by mobile (normalize first)
                var normalizedMobile = NormalizeMobile(emailOrMobile);
                userExists = await _context.Users
                    .AnyAsync(u => u.Mobile == normalizedMobile, cancellationToken);
            }

            return Result<bool>.Success(userExists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check user exists failed for {EmailOrMobile}", request.EmailOrMobile);
            return Result<bool>.Failure("An error occurred while checking user existence");
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