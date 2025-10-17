// ============================================
// File: DevPioneers.Application/Common/Mappings/WalletMappings.cs
// ============================================
using DevPioneers.Application.Features.Wallet.DTOs;
using DevPioneers.Domain.Entities;

namespace DevPioneers.Application.Common.Mappings;

public static class WalletMappings
{
    public static WalletDto ToDto(this Wallet wallet)
    {
        return new WalletDto
        {
            Id = wallet.Id,
            UserId = wallet.UserId,
            Balance = wallet.Balance,
            Points = wallet.Points,
            Currency = wallet.Currency,
            CreatedAtUtc = wallet.CreatedAtUtc,
            UpdatedAtUtc = wallet.UpdatedAtUtc
        };
    }

    public static TransactionDto ToDto(this WalletTransaction transaction)
    {
        return new TransactionDto
        {
            Id = transaction.Id,
            WalletId = transaction.WalletId,
            Type = transaction.Type,
            Amount = transaction.Amount,
            Currency = transaction.Currency,
            BalanceBefore = transaction.BalanceBefore,
            BalanceAfter = transaction.BalanceAfter,
            Description = transaction.Description,
            RelatedEntityType = transaction.RelatedEntityType,
            RelatedEntityId = transaction.RelatedEntityId,
            TransferToUserId = transaction.TransferToUserId,
            CreatedAtUtc = transaction.CreatedAtUtc,
            UpdatedAtUtc = transaction.UpdatedAtUtc
        };
    }
}
