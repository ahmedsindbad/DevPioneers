// ============================================
// File: DevPioneers.Application/Features/Wallet/Commands/DebitWalletCommand.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Wallet.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Wallet.Commands;

public record DebitWalletCommand(
    int UserId,
    decimal Amount,
    string Currency,
    string Description,
    string? RelatedEntityType = null,
    int? RelatedEntityId = null
) : IRequest<Result<TransactionDto>>;
