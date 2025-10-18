// ============================================
// File: DevPioneers.Application/Features/Wallet/Commands/AddPointsCommand.cs
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

/// <summary>
/// Command to add points to user's wallet
/// </summary>
public record AddPointsCommand(
    int UserId,
    int Points,
    string Description,
    string? RelatedEntityType = null,
    int? RelatedEntityId = null
) : IRequest<Result<TransactionDto>>;