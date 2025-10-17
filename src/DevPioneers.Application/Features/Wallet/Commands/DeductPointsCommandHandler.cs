// ============================================
// File: DevPioneers.Application/Features/Wallet/Commands/DeductPointsCommandHandler.cs
// ============================================
using DevPioneers.Application.Common.Interfaces;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Wallet.DTOs;
using DevPioneers.Domain.Entities;
using DevPioneers.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DevPioneers.Application.Features.Wallet.Commands;

public class DeductPointsCommandHandler : IRequestHandler<DeductPointsCommand, Result<TransactionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<DeductPointsCommandHandler> _logger;
    private readonly IDateTime _dateTime;

    public DeductPointsCommandHandler(
        IApplicationDbContext context,
        ILogger<DeductPointsCommandHandler> logger,
        IDateTime dateTime)
    {
        _context = context;
        _logger = logger;
        _dateTime = dateTime;
    }

    public async Task<Result<TransactionDto>> Handle(DeductPointsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Points <= 0)
            {
                return Result<TransactionDto>.Failure("Points must be greater than zero");
            }

            // Get wallet
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);

            if (wallet == null)
            {
                return Result<TransactionDto>.Failure("Wallet not found");
            }

            // Check sufficient points
            if (wallet.Points < request.Points)
            {
                return Result<TransactionDto>.Failure("Insufficient points balance");
            }

            // Record points before transaction
            var pointsBefore = wallet.Points;

            // Deduct points
            wallet.RemovePoints(request.Points);

            // Create transaction record
            var transaction = new WalletTransaction
            {
                WalletId = wallet.Id,
                Type = TransactionType.Debit,
                Amount = request.Points,
                Currency = "PTS",
                BalanceBefore = pointsBefore,
                BalanceAfter = wallet.Points,
                Description = request.Description,
                RelatedEntityType = request.RelatedEntityType,
                RelatedEntityId = request.RelatedEntityId,
                CreatedAtUtc = _dateTime.UtcNow
            };

            _context.WalletTransactions.Add(transaction);
            await _context.SaveChangesAsync(cancellationToken);

            var transactionDto = new TransactionDto
            {
                Id = transaction.Id,
                WalletId = wallet.Id,
                UserId = request.UserId,
                Type = transaction.Type,
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                BalanceBefore = transaction.BalanceBefore,
                BalanceAfter = transaction.BalanceAfter,
                Description = transaction.Description,
                RelatedEntityType = request.RelatedEntityType,
                RelatedEntityId = request.RelatedEntityId,
                CreatedAtUtc = transaction.CreatedAtUtc
            };

            _logger.LogInformation("Points deducted successfully for user {UserId}, points {Points}", 
                request.UserId, request.Points);

            return Result<TransactionDto>.Success(transactionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deduct points for user {UserId}", request.UserId);
            return Result<TransactionDto>.Failure("An error occurred while deducting points");
        }
    }
}
