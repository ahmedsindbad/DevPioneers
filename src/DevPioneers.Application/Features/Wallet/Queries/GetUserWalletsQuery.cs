// ============================================
// File: DevPioneers.Application/Features/Wallet/Queries/GetUserWalletsQuery.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Wallet.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Wallet.Queries;

/// <summary>
/// Query to get paginated list of user wallets (Admin only)
/// </summary>
public record GetUserWalletsQuery(
    int PageNumber = 1,
    int PageSize = 20,
    string? SearchTerm = null,
    bool? IsActive = null,
    decimal? MinBalance = null,
    decimal? MaxBalance = null
) : IRequest<Result<PaginatedList<WalletDto>>>;

public class GetUserWalletsQueryHandler : IRequestHandler<GetUserWalletsQuery, Result<PaginatedList<WalletDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetUserWalletsQueryHandler> _logger;

    public GetUserWalletsQueryHandler(
        IApplicationDbContext context,
        ILogger<GetUserWalletsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<PaginatedList<WalletDto>>> Handle(GetUserWalletsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Build query
            IQueryable<DevPioneers.Domain.Entities.Wallet>  query = _context.Wallets
                .AsNoTracking()
                .Include(w => w.User);

            // Apply filters
            if (request.IsActive.HasValue)
            {
                query = query.Where(w => w.IsActive == request.IsActive.Value);
            }

            if (request.MinBalance.HasValue)
            {
                query = query.Where(w => w.Balance >= request.MinBalance.Value);
            }

            if (request.MaxBalance.HasValue)
            {
                query = query.Where(w => w.Balance <= request.MaxBalance.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.Trim().ToLower();
                query = query.Where(w => w.User.Email.ToLower().Contains(searchTerm) ||
                                         w.User.FullName.ToLower().Contains(searchTerm) ||
                                         (w.User.Mobile != null && w.User.Mobile.Contains(searchTerm)));
            }

            // Order by balance descending
            query = query.OrderByDescending(w => w.Balance)
                        .ThenByDescending(w => w.UpdatedAtUtc);

            // Get total count for pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination and select
            var wallets = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(w => new WalletDto
                {
                    Id = w.Id,
                    UserId = w.UserId,
                    Balance = w.Balance,
                    Points = w.Points,
                    Currency = w.Currency,
                    TotalEarned = w.TotalEarned,
                    TotalSpent = w.TotalSpent,
                    IsActive = w.IsActive,
                    CreatedAtUtc = w.CreatedAtUtc,
                    UpdatedAtUtc = w.UpdatedAtUtc,
                    // Include user info for admin
                    UserEmail = w.User.Email,
                    UserFullName = w.User.FullName,
                    UserMobile = w.User.Mobile
                })
                .ToListAsync(cancellationToken);

            var paginatedResult = new PaginatedList<WalletDto>(
                wallets,
                totalCount,
                request.PageNumber,
                request.PageSize);

            _logger.LogInformation("Retrieved {Count} wallets for admin ^(page {PageNumber}^)", 
                wallets.Count, request.PageNumber);

            return Result<PaginatedList<WalletDto>>.Success(paginatedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get user wallets list");
            return Result<PaginatedList<WalletDto>>.Failure("An error occurred while retrieving user wallets");
        }
    }
}
