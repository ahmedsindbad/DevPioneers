// ============================================
// File: DevPioneers.Application/Features/Subscriptions/Commands/CreateSubscriptionCommand.cs
// ============================================
using DevPioneers.Application.Common.Models;
using DevPioneers.Application.Features.Subscriptions.DTOs;
using MediatR;

namespace DevPioneers.Application.Features.Subscriptions.Commands;

public record CreateSubscriptionCommand(
    int UserId,
    int SubscriptionPlanId,
    int? PaymentId = null
) : IRequest<Result<SubscriptionDto>>;