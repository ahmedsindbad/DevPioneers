// ============================================
// File: DevPioneers.Application/Features/Wallet/Commands/TransferPointsCommand.cs
// ============================================
using DevPioneers.Application.Common.Attributes;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Wallet.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Wallet.Commands;

/// <summary>
/// Command to transfer points between wallets
/// </summary>
/// [RequireWalletAccess]
[RequireOwnership("FromUserId")] // User can only transfer from their own wallet
public record TransferPointsCommand(
    int FromUserId,
    int ToUserId,
    int Points,
    string Description
) : IRequest<Result<TransferResultDto>>;
