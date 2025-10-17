// ============================================
// File: DevPioneers.Application/Features/Wallet/Queries/GetWalletBalanceQuery.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Wallet.DTOs;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Wallet.Queries;

/// <summary>
/// Query to get wallet balance for a specific user
/// </summary>
public record GetWalletBalanceQuery(int UserId) : IRequest<Result<WalletDto>>;

public class GetWalletBalanceQueryHandler : IRequestHandler<GetWalletBalanceQuery, Result<WalletDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetWalletBalanceQueryHandler> _logger;

    public GetWalletBalanceQueryHandler(
        IApplicationDbContext context,
        ILogger<GetWalletBalanceQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<WalletDto>> Handle(GetWalletBalanceQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var wallet = await _context.Wallets
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);

            if (wallet == null)
            {
                _logger.LogWarning("Wallet not found for user {UserId}", request.UserId);
                return Result<WalletDto>.Failure("Wallet not found");
            }

            var walletDto = new WalletDto
            {
                Id = wallet.Id,
                UserId = wallet.UserId,
                Balance = wallet.Balance,
                Points = wallet.Points,
                Currency = wallet.Currency,
                TotalEarned = wallet.TotalEarned,
                TotalSpent = wallet.TotalSpent,
                IsActive = wallet.IsActive,
                CreatedAtUtc = wallet.CreatedAtUtc,
                UpdatedAtUtc = wallet.UpdatedAtUtc
            };

            _logger.LogInformation("Retrieved wallet balance for user {UserId}: {Balance} {Currency}", 
                request.UserId, wallet.Balance, wallet.Currency);

            return Result<WalletDto>.Success(walletDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get wallet balance for user {UserId}", request.UserId);
            return Result<WalletDto>.Failure("An error occurred while retrieving wallet balance");
        }
    }
}
