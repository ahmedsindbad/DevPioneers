// ============================================
// File: DevPioneers.Application/Features/Wallet/Commands/TransferPointsCommand.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Wallet.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Wallet.Commands;

public record TransferPointsCommand(
    int FromUserId,
    int ToUserId,
    int Points,
    string Description
) : IRequest<Result<TransferResultDto>>;
