// ============================================
// File: DevPioneers.Application/Features/Wallet/Commands/AddPointsCommand.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Wallet.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Wallet.Commands;

public record AddPointsCommand(
    int UserId,
    int Points,
    string Description,
    string? RelatedEntityType = null,
    int? RelatedEntityId = null
) : IRequest<Result<TransactionDto>>;
