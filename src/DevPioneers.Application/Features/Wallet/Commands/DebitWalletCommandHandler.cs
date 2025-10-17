// ============================================
// File: DevPioneers.Application/Features/Wallet/Commands/DebitWalletCommandHandler.cs
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

public class DebitWalletCommandHandler : IRequestHandler<DebitWalletCommand, Result<TransactionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<DebitWalletCommandHandler> _logger;
    private readonly IDateTime _dateTime;

    public DebitWalletCommandHandler(
        IApplicationDbContext context,
        ILogger<DebitWalletCommandHandler> logger,
        IDateTime dateTime)
    {
        _context = context;
        _logger = logger;
        _dateTime = dateTime;
    }

    public async Task<Result<TransactionDto>> Handle(DebitWalletCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Amount <= 0)
            {
                return Result<TransactionDto>.Failure("Amount must be greater than zero");
            }

            // Get wallet
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);

            if (wallet == null)
            {
                return Result<TransactionDto>.Failure("Wallet not found");
            }

            // Check sufficient balance
            if (wallet.Balance < request.Amount)
            {
                return Result<TransactionDto>.Failure("Insufficient wallet balance");
            }

            // Record balance before transaction
            var balanceBefore = wallet.Balance;

            // Debit wallet
            wallet.Debit(request.Amount, request.Description);

            // Create transaction record
            var transaction = new WalletTransaction
            {
                WalletId = wallet.Id,
                Type = TransactionType.Debit,
                Amount = request.Amount,
                Currency = request.Currency,
                BalanceBefore = balanceBefore,
                BalanceAfter = wallet.Balance,
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

            _logger.LogInformation("Wallet debited successfully for user {UserId}, amount {Amount} {Currency}", 
                request.UserId, request.Amount, request.Currency);

            return Result<TransactionDto>.Success(transactionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to debit wallet for user {UserId}", request.UserId);
            return Result<TransactionDto>.Failure("An error occurred while debiting wallet");
        }
    }
}
