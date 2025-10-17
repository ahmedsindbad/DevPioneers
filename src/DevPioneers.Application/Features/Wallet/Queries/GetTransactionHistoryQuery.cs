// ============================================
// File: DevPioneers.Application/Features/Wallet/Queries/GetTransactionHistoryQuery.cs
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
/// Query to get paginated transaction history for a user's wallet
/// </summary>
public record GetTransactionHistoryQuery(
    int UserId,
    int PageNumber = 1,
    int PageSize = 20,
    TransactionType? Type = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    string? SearchTerm = null
) : IRequest<Result<PaginatedList<TransactionDto>>>;

public class GetTransactionHistoryQueryHandler : IRequestHandler<GetTransactionHistoryQuery, Result<PaginatedList<TransactionDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<GetTransactionHistoryQueryHandler> _logger;

    public GetTransactionHistoryQueryHandler(
        IApplicationDbContext context,
        ILogger<GetTransactionHistoryQueryHandler> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Result<PaginatedList<TransactionDto>>> Handle(GetTransactionHistoryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // First, verify wallet exists
            var walletExists = await _context.Wallets
                .AsNoTracking()
                .AnyAsync(w => w.UserId == request.UserId, cancellationToken);

            if (!walletExists)
            {
                _logger.LogWarning("Wallet not found for user {UserId}", request.UserId);
                return Result<PaginatedList<TransactionDto>>.Failure("Wallet not found");
            }

            // Build query
            var query = _context.WalletTransactions
                .AsNoTracking()
                .Include(t => t.Wallet)
                .Where(t => t.Wallet.UserId == request.UserId);

            // Apply filters
            if (request.Type.HasValue)
            {
                query = query.Where(t => t.Type == request.Type.Value);
            }

            if (request.FromDate.HasValue)
            {
                query = query.Where(t => t.CreatedAtUtc >= request.FromDate.Value);
            }

            if (request.ToDate.HasValue)
            {
                query = query.Where(t => t.CreatedAtUtc <= request.ToDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTerm))
            {
                var searchTerm = request.SearchTerm.Trim().ToLower();
                query = query.Where(t => t.Description.ToLower().Contains(searchTerm) ||
                                         (t.RelatedEntityType != null && t.RelatedEntityType.ToLower().Contains(searchTerm)));
            }

            // Order by created date descending (newest first)
            query = query.OrderByDescending(t => t.CreatedAtUtc);

            // Get total count for pagination
            var totalCount = await query.CountAsync(cancellationToken);

            // Apply pagination
            var transactions = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(t => new TransactionDto
                {
                    Id = t.Id,
                    WalletId = t.WalletId,
                    Type = t.Type,
                    Amount = t.Amount,
                    Currency = t.Currency,
                    Points = t.Points,
                    BalanceBefore = t.BalanceBefore,
                    BalanceAfter = t.BalanceAfter,
                    Description = t.Description,
                    RelatedEntityId = t.RelatedEntityId,
                    RelatedEntityType = t.RelatedEntityType,
                    TransferToUserId = t.TransferToUserId,
                    Metadata = t.Metadata,
                    CreatedAtUtc = t.CreatedAtUtc
                })
                .ToListAsync(cancellationToken);

            var paginatedResult = new PaginatedList<TransactionDto>(
                transactions,
                totalCount,
                request.PageNumber,
                request.PageSize);

            _logger.LogInformation("Retrieved {Count} transactions for user {UserId} ^(page {PageNumber}^)", 
                transactions.Count, request.UserId, request.PageNumber);

            return Result<PaginatedList<TransactionDto>>.Success(paginatedResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get transaction history for user {UserId}", request.UserId);
            return Result<PaginatedList<TransactionDto>>.Failure("An error occurred while retrieving transaction history");
        }
    }
}
