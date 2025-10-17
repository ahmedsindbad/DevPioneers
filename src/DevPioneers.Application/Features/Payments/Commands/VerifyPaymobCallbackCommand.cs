// ============================================
// File: DevPioneers.Application/Features/Payments/Commands/VerifyPaymobCallbackCommand.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Payments.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Payments.Commands;

public record VerifyPaymobCallbackCommand(
    string PaymobOrderId,
    string PaymobTransactionId,
    string Status,
    decimal Amount,
    string Currency,
    Dictionary<string, object>? AdditionalData = null
) : IRequest<Result<PaymentVerificationDto>>;
