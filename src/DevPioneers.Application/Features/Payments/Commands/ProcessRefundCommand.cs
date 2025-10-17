// ============================================
// File: DevPioneers.Application/Features/Payments/Commands/ProcessRefundCommand.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Payments.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Payments.Commands;

public record ProcessRefundCommand(
    int PaymentId,
    decimal Amount,
    string Reason,
    int ProcessedByUserId
) : IRequest<Result<RefundDto>>;

