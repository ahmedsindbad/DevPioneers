// ============================================
// File: DevPioneers.Application/Features/Payments/Commands/CreatePaymobOrderCommand.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Payments.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Payments.Commands;

public record CreatePaymobOrderCommand(
    int UserId,
    decimal Amount,
    string Currency,
    string Description,
    int? SubscriptionPlanId = null,
    string? IpAddress = null,
    string? UserAgent = null
) : IRequest<Result<PaymobOrderDto>>;