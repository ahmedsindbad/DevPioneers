// ============================================
// File: DevPioneers.Application/Features/Wallet/Commands/DeductPointsCommand.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Wallet.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Wallet.Commands;

public record DeductPointsCommand(
    int UserId,
    int Points,
    string Description,
    string? RelatedEntityType = null,
    int? RelatedEntityId = null
) : IRequest<Result<TransactionDto>>;
