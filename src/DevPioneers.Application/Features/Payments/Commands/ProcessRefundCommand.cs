// ============================================
// File: DevPioneers.Application/Features/Payments/Commands/ProcessRefundCommand.cs
// ============================================
using DevPioneers.Application.Common.Attributes;
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Payments.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Payments.Commands;

[RequirePaymentManagement]
[RequireResourceAccess("Payment", "Write")]
// [RequireIpAddress("192.168.1.0/24", "10.0.0.0/8")] // Only from office networks
public record ProcessRefundCommand(
    int PaymentId,
    decimal Amount,
    string Reason,
    int ProcessedByUserId
) : IRequest<Result<RefundDto>>;

