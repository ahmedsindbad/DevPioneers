// ============================================
// File: DevPioneers.Application/Features/Wallet/Commands/TransferPointsCommandHandler.cs
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

public class TransferPointsCommandHandler : IRequestHandler<TransferPointsCommand, Result<TransferResultDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<TransferPointsCommandHandler> _logger;
    private readonly IDateTime _dateTime;

    public TransferPointsCommandHandler(
        IApplicationDbContext context,
        ILogger<TransferPointsCommandHandler> logger,
        IDateTime dateTime)
    {
        _context = context;
        _logger = logger;
        _dateTime = dateTime;
    }

    public async Task<Result<TransferResultDto>> Handle(TransferPointsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            if (request.Points <= 0)
            {
                return Result<TransferResultDto>.Failure("Points must be greater than zero");
            }

            if (request.FromUserId == request.ToUserId)
            {
                return Result<TransferResultDto>.Failure("Cannot transfer points to yourself");
            }

            // Get both wallets
            var fromWallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == request.FromUserId, cancellationToken);

            var toWallet = await _context.Wallets
                .FirstOrDefaultAsync(w => w.UserId == request.ToUserId, cancellationToken);

            if (fromWallet == null)
            {
                return Result<TransferResultDto>.Failure("Source wallet not found");
            }

            if (toWallet == null)
            {
                return Result<TransferResultDto>.Failure("Destination wallet not found");
            }

            // Check sufficient points
            if (fromWallet.Points < request.Points)
            {
                return Result<TransferResultDto>.Failure("Insufficient points balance");
            }

            // Record balances before transfer
            var fromPointsBefore = fromWallet.Points;
            var toPointsBefore = toWallet.Points;

            // Transfer points
            fromWallet.RemovePoints(request.Points, $"Points transferred to user {request.ToUserId}: {request.Description}");
            toWallet.AddPoints(request.Points, $"Points received from user {request.FromUserId}: {request.Description}");

            // Create transaction records
            var fromTransaction = new WalletTransaction
            {
                WalletId = fromWallet.Id,
                Type = TransactionType.PointsDebit,
                Amount = request.Points,
                Currency = "PTS",
                BalanceBefore = fromPointsBefore,
                BalanceAfter = fromWallet.Points,
                Description = $"Points transferred to user {request.ToUserId}: {request.Description}",
                TransferToUserId = request.ToUserId,
                CreatedAtUtc = _dateTime.UtcNow
            };

            var toTransaction = new WalletTransaction
            {
                WalletId = toWallet.Id,
                Type = TransactionType.PointsCredit,
                Amount = request.Points,
                Currency = "PTS",
                BalanceBefore = toPointsBefore,
                BalanceAfter = toWallet.Points,
                Description = $"Points received from user {request.FromUserId}: {request.Description}",
                CreatedAtUtc = _dateTime.UtcNow
            };

            _context.WalletTransactions.AddRange(fromTransaction, toTransaction);
            await _context.SaveChangesAsync(cancellationToken);

            var transferResult = new TransferResultDto
            {
                FromUserId = request.FromUserId,
                ToUserId = request.ToUserId,
                Points = request.Points,
                Description = request.Description,
                FromTransactionId = fromTransaction.Id,
                ToTransactionId = toTransaction.Id,
                FromBalanceAfter = fromWallet.Points,
                ToBalanceAfter = toWallet.Points,
                TransferredAt = _dateTime.UtcNow
            };

            _logger.LogInformation("Points transferred successfully from user {FromUserId} to user {ToUserId}, points {Points}", 
                request.FromUserId, request.ToUserId, request.Points);

            return Result<TransferResultDto>.Success(transferResult);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transfer points from user {FromUserId} to user {ToUserId}", 
                request.FromUserId, request.ToUserId);
            return Result<TransferResultDto>.Failure("An error occurred while transferring points");
        }
    }
}
