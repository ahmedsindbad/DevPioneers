// ============================================
// File: DevPioneers.Application/Features/Wallet/Queries/GetComprehensiveWalletStatisticsQuery.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Wallet.DTOs;
using DevPioneers.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Wallet.Queries;

/// <summary>
/// Query to get comprehensive wallet statistics for admin dashboard
/// </summary>
public record GetComprehensiveWalletStatisticsQuery() : IRequest<Result<WalletStatsDto>>;

public class GetComprehensiveWalletStatisticsQueryHandler : IRequestHandler<GetComprehensiveWalletStatisticsQuery, Result<WalletStatsDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetComprehensiveWalletStatisticsQueryHandler> _logger;

    public GetComprehensiveWalletStatisticsQueryHandler(
        IApplicationDbContext context,
        ILogger<GetComprehensiveWalletStatisticsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<WalletStatsDto>> Handle(GetComprehensiveWalletStatisticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get wallet statistics
            var totalWallets = await _context.Wallets.CountAsync(cancellationToken);
            var activeWallets = await _context.Wallets.CountAsync(w => w.IsActive, cancellationToken);
            var emptyWallets = await _context.Wallets.CountAsync(w => w.Balance == 0 && w.Points == 0, cancellationToken);

            var totalBalance = await _context.Wallets.SumAsync(w => w.Balance, cancellationToken);
            var totalPoints = await _context.Wallets.SumAsync(w => w.Points, cancellationToken);
            var averageBalance = totalWallets > 0 ? totalBalance / totalWallets : 0;
            var averagePoints = totalWallets > 0 ? totalPoints / totalWallets : 0;

            // Get transaction statistics
            var totalTransactions = await _context.WalletTransactions.CountAsync(cancellationToken);
            var creditTransactions = await _context.WalletTransactions.CountAsync(t => t.Type == TransactionType.Credit, cancellationToken);
            var debitTransactions = await _context.WalletTransactions.CountAsync(t => t.Type == TransactionType.Debit, cancellationToken);
            var pointsTransactions = await _context.WalletTransactions.CountAsync(t => t.Currency == "PTS", cancellationToken);
            var transferTransactions = await _context.WalletTransactions.CountAsync(t => t.Type == TransactionType.Transfer, cancellationToken);

            // Get top wallets by balance
            var topWalletsByBalance = await _context.Wallets
                .Include(w => w.User)
                .OrderByDescending(w => w.Balance)
                .Take(10)
                .Select(w => new TopWalletDto
                {
                    UserId = w.UserId,
                    UserName = w.User.FullName ?? w.User.Email,
                    Balance = w.Balance,
                    Points = w.Points
                })
                .ToListAsync(cancellationToken);

            // Get top wallets by points
            var topWalletsByPoints = await _context.Wallets
                .Include(w => w.User)
                .OrderByDescending(w => w.Points)
                .Take(10)
                .Select(w => new TopWalletDto
                {
                    UserId = w.UserId,
                    UserName = w.User.FullName ?? w.User.Email,
                    Balance = w.Balance,
                    Points = w.Points
                })
                .ToListAsync(cancellationToken);

            // Get recent transactions
            var recentTransactions = await _context.WalletTransactions
                .Include(t => t.Wallet)
                .ThenInclude(w => w.User)
                .OrderByDescending(t => t.CreatedAtUtc)
                .Take(20)
                .Select(t => new RecentTransactionDto
                {
                    TransactionId = t.Id,
                    UserId = t.Wallet.UserId,
                    UserName = t.Wallet.User.FullName ?? t.Wallet.User.Email,
                    Type = t.Type.ToString(),
                    Amount = t.Amount,
                    Currency = t.Currency,
                    Description = t.Description,
                    CreatedAt = t.CreatedAtUtc
                })
                .ToListAsync(cancellationToken);

            var statsDto = new WalletStatsDto
            {
                TotalWallets = totalWallets,
                ActiveWallets = activeWallets,
                EmptyWallets = emptyWallets,
                TotalBalance = totalBalance,
                TotalPoints = (int)totalPoints,
                TotalValue = totalBalance + (totalPoints / 10m), // Assuming 10 points = 1 EGP
                Currency = "EGP",
                AverageBalance = averageBalance,
                AveragePoints = (int)averagePoints,
                AverageValue = averageBalance + (averagePoints / 10m),
                TotalTransactions = totalTransactions,
                CreditTransactions = creditTransactions,
                DebitTransactions = debitTransactions,
                PointsTransactions = pointsTransactions,
                TransferTransactions = transferTransactions,
                TopWalletsByBalance = topWalletsByBalance,
                TopWalletsByPoints = topWalletsByPoints,
                RecentTransactions = recentTransactions
            };

            _logger.LogInformation("Retrieved comprehensive wallet statistics: {TotalWallets} wallets, {TotalBalance} total balance",
                totalWallets, totalBalance);

            return Result<WalletStatsDto>.Success(statsDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get comprehensive wallet statistics");
            return Result<WalletStatsDto>.Failure("An error occurred while retrieving wallet statistics");
        }
    }
}