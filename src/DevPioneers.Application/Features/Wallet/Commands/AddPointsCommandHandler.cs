// ============================================
// File: DevPioneers.Application/Features/Wallet/Commands/AddPointsCommandHandler.cs
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

public class AddPointsCommandHandler : IRequestHandler<AddPointsCommand, Result<TransactionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<AddPointsCommandHandler> _logger;
    private readonly IDateTime _dateTime;

    public AddPointsCommandHandler(
        IApplicationDbContext context,
        ILogger<AddPointsCommandHandler> logger,
        IDateTime dateTime)
    {
        _context = context;
        _logger = logger;
        _dateTime = dateTime;
    }

    public async Task<Result<TransactionDto>> Handle(AddPointsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Points <= 0)
            {
                return Result<TransactionDto>.Failure("Points must be greater than zero");
            }

            // Get or create wallet
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);

            if (wallet == null)
            {
                return Result<TransactionDto>.Failure("Wallet not found");
            }

            // Record points before transaction
            var pointsBefore = wallet.Points;

            // Add points
            wallet.AddPoints(request.Points, request.Description);

            // Create transaction record
            var transaction = new WalletTransaction
            {
                WalletId = wallet.Id,
                Type = TransactionType.PointsCredit,
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
                Type = transaction.Type.ToString(),
                Amount = transaction.Amount,
                Currency = transaction.Currency,
                BalanceBefore = transaction.BalanceBefore,
                BalanceAfter = transaction.BalanceAfter,
                Description = transaction.Description,
                RelatedEntityType = transaction.RelatedEntityType,
                RelatedEntityId = transaction.RelatedEntityId,
                CreatedAtUtc = transaction.CreatedAtUtc
            };

            _logger.LogInformation("Points added successfully for user {UserId}, points {Points}", 
                request.UserId, request.Points);

            return Result<TransactionDto>.Success(transactionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add points for user {UserId}", request.UserId);
            return Result<TransactionDto>.Failure("An error occurred while adding points");
        }
    }
}
