// ============================================
// File: DevPioneers.Application/Features/Wallet/Queries/GetWalletStatisticsQuery.cs
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
/// Query to get wallet statistics for a specific period
/// </summary>
public record GetWalletStatisticsQuery(
    int UserId,
    DateTime? FromDate = null,
    DateTime? ToDate = null
) : IRequest<Result<WalletStatisticsDto>>;

public class GetWalletStatisticsQueryHandler : IRequestHandler<GetWalletStatisticsQuery, Result<WalletStatisticsDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetWalletStatisticsQueryHandler> _logger;

    public GetWalletStatisticsQueryHandler(
        IApplicationDbContext context,
        ILogger<GetWalletStatisticsQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<WalletStatisticsDto>> Handle(GetWalletStatisticsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Get wallet
            var wallet = await _context.Wallets
                .AsNoTracking()
                .FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);

            if (wallet == null)
            {
                _logger.LogWarning("Wallet not found for user {UserId}", request.UserId);
                return Result<WalletStatisticsDto>.Failure("Wallet not found");
            }

            // Default date range if not provided (last 30 days)
            var fromDate = request.FromDate ?? DateTime.UtcNow.AddDays(-30);
            var toDate = request.ToDate ?? DateTime.UtcNow;

            // Build transactions query for the period
            var transactionsQuery = _context.WalletTransactions
                .AsNoTracking()
                .Where(t => t.WalletId == wallet.Id && 
                            t.CreatedAtUtc >= fromDate && 
                            t.CreatedAtUtc <= toDate);

            // Calculate statistics
            var totalCredits = await transactionsQuery
                .Where(t => t.Type == TransactionType.Credit || 
                            t.Type == TransactionType.Refund || 
                            t.Type == TransactionType.PointsReward)
                .SumAsync(t => t.Amount, cancellationToken);

            var totalDebits = await transactionsQuery
                .Where(t => t.Type == TransactionType.Debit || 
                            t.Type == TransactionType.Transfer || 
                            t.Type == TransactionType.SubscriptionPayment)
                .SumAsync(t => t.Amount, cancellationToken);

            var transactionCount = await transactionsQuery.CountAsync(cancellationToken);

            var pointsEarned = await transactionsQuery
                .Where(t => t.Points.HasValue && t.Points > 0)
                .SumAsync(t => t.Points ?? 0, cancellationToken);

            var pointsSpent = await transactionsQuery
                .Where(t => t.Points.HasValue && t.Points < 0)
                .SumAsync(t => Math.Abs(t.Points ?? 0), cancellationToken);

            // Get transaction type breakdown
            var transactionsByType = await transactionsQuery
                .GroupBy(t => t.Type)
                .Select(g => new { Type = g.Key, Count = g.Count(), Total = g.Sum(x => x.Amount) })
                .ToListAsync(cancellationToken);

            var statisticsDto = new WalletStatisticsDto
            {
                UserId = request.UserId,
                WalletId = wallet.Id,
                CurrentBalance = wallet.Balance,
                CurrentPoints = wallet.Points,
                Currency = wallet.Currency,
                PeriodFromDate = fromDate,
                PeriodToDate = toDate,
                TotalCredits = totalCredits,
                TotalDebits = totalDebits,
                NetAmount = totalCredits - totalDebits,
                TransactionCount = transactionCount,
                PointsEarned = pointsEarned,
                PointsSpent = pointsSpent,
                NetPoints = pointsEarned - pointsSpent,
                LifetimeEarned = wallet.TotalEarned,
                LifetimeSpent = wallet.TotalSpent,
                TransactionsByType = transactionsByType.ToDictionary(
                    x => x.Type.ToString(), 
                    x => new { Count = x.Count, Total = x.Total })
            };

            _logger.LogInformation("Retrieved wallet statistics for user {UserId} for period {FromDate} to {ToDate}", 
                request.UserId, fromDate, toDate);

            return Result<WalletStatisticsDto>.Success(statisticsDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get wallet statistics for user {UserId}", request.UserId);
            return Result<WalletStatisticsDto>.Failure("An error occurred while retrieving wallet statistics");
        }
    }
}
