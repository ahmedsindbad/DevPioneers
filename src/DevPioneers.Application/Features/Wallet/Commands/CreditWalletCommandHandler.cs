// ============================================
// File: DevPioneers.Application/Features/Wallet/Commands/CreditWalletCommandHandler.cs
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

public class CreditWalletCommandHandler : IRequestHandler<CreditWalletCommand, Result<TransactionDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<CreditWalletCommandHandler> _logger;
    private readonly IDateTime _dateTime;

    public CreditWalletCommandHandler(
        IApplicationDbContext context,
        ILogger<CreditWalletCommandHandler> logger,
        IDateTime dateTime)
    {
        _context = context;
        _logger = logger;
        _dateTime = dateTime;
    }

    public async Task<Result<TransactionDto>> Handle(CreditWalletCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Amount <= 0)
            {
                return Result<TransactionDto>.Failure("Amount must be greater than zero");
            }

            // Get or create wallet
            var wallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == request.UserId, cancellationToken);

            if (wallet == null)
            {
                // Create wallet if it doesn't exist
                wallet = new Domain.Entities.Wallet
                {
                    UserId = request.UserId,
                    Balance = 0,
                    Points = 0,
                    Currency = request.Currency,
                    CreatedAtUtc = _dateTime.UtcNow
                };
                _context.Wallets.Add(wallet);
                await _context.SaveChangesAsync(cancellationToken);
            }

            // Record balance before transaction
            var balanceBefore = wallet.Balance;

            // Credit wallet
            wallet.Credit(request.Amount, request.Description);

            // Create transaction record
            var transaction = new WalletTransaction
            {
                WalletId = wallet.Id,
                Type = TransactionType.Credit,
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

            _logger.LogInformation("Wallet credited successfully for user {UserId}, amount {Amount} {Currency}", 
                request.UserId, request.Amount, request.Currency);

            return Result<TransactionDto>.Success(transactionDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to credit wallet for user {UserId}", request.UserId);
            return Result<TransactionDto>.Failure("An error occurred while crediting wallet");
        }
    }
}
